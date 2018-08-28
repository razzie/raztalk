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
            var setup = new AppDomainSetup();
            setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            setup.PrivateBinPath = AppDomain.CurrentDomain.BaseDirectory;

            Domain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, setup);
            Loader = (RemoteLoader)Domain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(RemoteLoader).FullName);
            Loader.Folder = Folder;

            Domain.AssemblyResolve += Loader.AssemblyResolve;
        }

        private void Load()
        {
            CreateDomain();

            var classes = new List<Type>();
            foreach (var file in DLLs)
            {
                var aname = AssemblyName.GetAssemblyName(file.FullName);
                var dll = RemoteLoadDLL(Loader, file.FullName); //Domain.Load(aname);
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
                    var dll = RemoteLoadDLL(Loader, file.FullName); //Domain.Load(aname);
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
