using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace raztalk
{
    public class DllDomainManager
    {
        public event EventHandler<AppDomain> Unloaded;

        private AppDomain Domain { get; set; }
        private string Folder { get; set; }
        private FileInfo[] DLLs { get { return new DirectoryInfo(Folder).GetFiles("*.dll"); } }
        private Dictionary<string, string> Assemblies { get; } = new Dictionary<string, string>();
        public Type[] Classes { get; private set; } = new Type[0];
        
        class AssemblyResolveProxy : MarshalByRefObject
        {
            private string Folder { get; set; }

            public AssemblyResolveProxy(string folder)
            {
                Folder = folder;
            }

            public Assembly AssemblyResolve(object o, ResolveEventArgs e)
            {
                string dll = new AssemblyName(e.Name).Name + ".dll";

                FileInfo file = new DirectoryInfo("/").GetFiles().First(f => f.Name.Equals(dll, StringComparison.InvariantCultureIgnoreCase));
                if (file != null)
                    return Assembly.LoadFile(file.Name);

                file = new DirectoryInfo(Folder).GetFiles().First(f => f.Name.Equals(dll, StringComparison.InvariantCultureIgnoreCase));
                if (file != null)
                    return Assembly.LoadFile(Folder + file.Name);

                return AppDomain.CurrentDomain.Load(e.Name);
            }
        }

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
            Domain = AppDomain.CreateDomain(Guid.NewGuid().ToString());
            Domain.AssemblyResolve += new AssemblyResolveProxy(Folder).AssemblyResolve;
        }

        private void Load()
        {
            CreateDomain();

            var classes = new List<Type>();
            foreach (var file in DLLs)
            {
                var aname = AssemblyName.GetAssemblyName(file.FullName);
                var dll = Domain.Load(aname);
                Assemblies.Add(file.FullName, aname.FullName);
                classes.AddRange(dll.GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(MarshalByRefObject))));
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
                    var dll = Domain.Load(aname);
                    Assemblies.Add(file.FullName, aname.FullName);
                    classes.AddRange(dll.GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract));
                }
            }

            Classes = classes.ToArray();
        }

        private void HardReload()
        {
            Unload();
            Load();
        }
    }
}
