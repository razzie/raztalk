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
        static private string CacheFolder { get; } = AppDomain.CurrentDomain.BaseDirectory + "cache/";
        private AppDomain Domain { get; set; }
        private RemoteLoader Loader { get; set; }
        private string Folder { get; set; }
        private FileInfo[] DLLs { get { return new DirectoryInfo(Folder).GetFiles("*.dll"); } }
        private Dictionary<string, string> Assemblies { get; } = new Dictionary<string, string>();
        public Type[] Classes { get; private set; } = new Type[0];

        public DllDomainManager(string folder)
        {
            Folder = folder;
            CreateDomain();
        }

        public MarshalByRefObject Create(string typename)
        {
            foreach (var type in Classes)
            {
                if (type.Name.Equals(typename))
                {
                    string assemblyname = type.Assembly.GetName().Name;
                    return (MarshalByRefObject)Domain.CreateInstanceAndUnwrap(assemblyname, typename);
                }
            }

            return null;
        }

        private void CreateDomain()
        {
            var setup = new AppDomainSetup()
            {
                ApplicationName = "raztalk",
                //ShadowCopyFiles = "true",
                //CachePath = CacheFolder,
                //ShadowCopyDirectories = CacheFolder,
                ApplicationBase = AppFolder,
                PrivateBinPath = AppFolder
            };

            var permissions = new PermissionSet(PermissionState.None);
            permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.PathDiscovery, AppFolder));
            //permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write | FileIOPermissionAccess.PathDiscovery, CacheFolder));
            permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution | SecurityPermissionFlag.ControlAppDomain));

            StrongName[] trust = new StrongName[0];

            Domain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), AppDomain.CurrentDomain.Evidence, setup, permissions, trust);
            Loader = (RemoteLoader)Domain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(RemoteLoader).FullName);
            Loader.Folder = Folder;
        }

        private void Load()
        {
            CreateDomain();

            var classes = new List<Type>();
            foreach (var file in DLLs)
            {
                var aname = AssemblyName.GetAssemblyName(file.FullName);
                var dll = RemoteLoadDLL(Loader, file.FullName);
                Assemblies.Add(file.FullName, aname.FullName);
                classes.AddRange(dll.Subclasses(typeof(MarshalByRefObject)));
            }

            Classes = classes.ToArray();
        }

        private void Unload()
        {
            Unloaded?.Invoke(this, Domain);
            Unloaded = null;

            AppDomain.Unload(Domain);
            Domain = null;

            Assemblies.Clear();
            Classes = null;
        }

        public void Reload()
        {
            var classes = new List<Type>();
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
                    var dll = RemoteLoadDLL(Loader, file.FullName);
                    Assemblies.Add(file.FullName, aname.FullName);
                    classes.AddRange(dll.Subclasses(typeof(MarshalByRefObject)));
                }
            }

            Classes = classes.ToArray();
        }

        private void HardReload()
        {
            Unload();
            Load();
        }

        static private Assembly RemoteLoadDLL(RemoteLoader loader, string dll)
        {
            return loader.LoadAssembly(dll);
        }
    }

    static class AssemblyExtensions
    {
        static public IEnumerable<Type> Subclasses(this Assembly assembly, Type basetype)
        {
            return assembly.GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(basetype));
        }
    }
}
