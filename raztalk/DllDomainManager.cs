/*
Copyright (C) Gábor "Razzie" Görzsöny
Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

namespace raztalk
{
    public class DllDomainManager
    {
        public event EventHandler<AppDomain> Unloaded;

        static private string AppFolder { get; } = AppDomain.CurrentDomain.BaseDirectory;
        private AppDomain Domain { get; set; }
        private RemoteLoader Loader { get; set; }
        private string Folder { get; set; }
        private FileInfo[] DLLs { get { return new DirectoryInfo(Folder).GetFiles("*.dll"); } }
        private Dictionary<string, string> Assemblies { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> Classes { get; } = new Dictionary<string, string>();

        public DllDomainManager(string folder)
        {
            Folder = folder;
            CreateDomain();
        }

        public MarshalByRefObject Create(string typename)
        {
            //foreach (var type in Classes)
            //{
            //    if (type.Name.Equals(typename))
            //    {
            //        string assemblyname = type.Assembly.GetName().Name;
            //        return (MarshalByRefObject)Domain.CreateInstanceAndUnwrap(assemblyname, typename);
            //    }
            //}

            string assembly;
            if (Classes.TryGetValue(typename, out assembly))
            {
                return (MarshalByRefObject)Domain.CreateInstanceAndUnwrap(assembly, typename);
            }

            return null;
        }

        private void CreateDomain()
        {
            var setup = new AppDomainSetup()
            {
                ApplicationName = "raztalk",
                //ShadowCopyFiles = "true",
                //CachePath = AppFolder,
                //ShadowCopyDirectories = AppFolder,
                ApplicationBase = AppFolder,
                PrivateBinPath = AppFolder
            };

            //var permissions = new PermissionSet(PermissionState.None);
            //permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery, AppFolder));
            //permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            //permissions.AddPermission(new ReflectionPermission(PermissionState.Unrestricted));

            //var signed_assemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Where(a => a.GetPublicKeyToken() != null);
            //StrongName[] trust = signed_assemblies.Select(a => new StrongName(new StrongNamePublicKeyBlob(a.GetPublicKeyToken()), a.Name, a.Version)).ToArray();

            Domain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), AppDomain.CurrentDomain.Evidence, setup, Assembly.GetExecutingAssembly().PermissionSet);
            Loader = (RemoteLoader)Domain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(RemoteLoader).FullName);
            Loader.Folder = Folder;
        }

        private void Load()
        {
            CreateDomain();

            foreach (var file in DLLs)
            {
                var aname = AssemblyName.GetAssemblyName(file.FullName);
                foreach (var typename in RemoteLoadAssemblyClasses(Loader, aname))
                {
                    Classes.Add(typename, aname.Name);
                }
                Assemblies.Add(file.FullName, aname.FullName);
            }
        }

        private void Unload()
        {
            Unloaded?.Invoke(this, Domain);
            Unloaded = null;

            AppDomain.Unload(Domain);
            Domain = null;

            Assemblies.Clear();
            Classes.Clear();
        }

        public void Reload()
        {
            foreach (var file in DLLs)
            {
                var aname = AssemblyName.GetAssemblyName(file.FullName);
                string cached_aname;
                if (Assemblies.TryGetValue(file.FullName, out cached_aname))
                {
                    if (!aname.FullName.Equals(cached_aname))
                    {
                        HardReload();
                        return;
                    }
                }
                else
                {
                    foreach (var typename in RemoteLoadAssemblyClasses(Loader, aname))
                    {
                        Classes.Add(typename, aname.Name);
                    }
                    Assemblies.Add(file.FullName, aname.FullName);
                }
            }
        }

        private void HardReload()
        {
            Unload();
            Load();
        }

        static private string[] RemoteLoadAssemblyClasses(RemoteLoader loader, AssemblyName assembly)
        {
            return loader.LoadAssemblyClasses(assembly);
        }
    }
}
