using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace raztalk
{
    public class RemoteLoader : MarshalByRefObject
    {
        public string Folder { get; set; }

        public Assembly LoadAssembly(string assemblyPath)
        {
            return Assembly.LoadFile(assemblyPath);
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
}
