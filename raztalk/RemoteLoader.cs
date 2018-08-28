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

namespace raztalk
{
    public class RemoteLoader : MarshalByRefObject
    {
        public string Folder { get; set; }
        public AppDomain Domain { get { return AppDomain.CurrentDomain; } }
        private Dictionary<AssemblyName, Assembly> LoadedAssemblies { get; } = new Dictionary<AssemblyName, Assembly>();

        static private FileInfo FindDLL(string dir, AssemblyName assembly)
        {
            string dll = assembly.Name + ".dll";
            return new DirectoryInfo(dir).GetFiles().FirstOrDefault(f => f.Name.Equals(dll, StringComparison.InvariantCultureIgnoreCase));
        }

        private FileInfo FindDLL(AssemblyName assembly)
        {
            return FindDLL(Domain.BaseDirectory, assembly) ?? FindDLL(Domain.BaseDirectory + Folder, assembly);
        }

        private Assembly LoadAssembly(AssemblyName assembly)
        {
            if (LoadedAssemblies.ContainsKey(assembly))
            {
                return LoadedAssemblies[assembly];
            }

            FileInfo dll = FindDLL(assembly);
            if (dll != null)
            {
                return LoadDependencies(Assembly.LoadFile(dll.FullName));
            }

            return Assembly.Load(assembly);
        }

        private Assembly LoadDependencies(Assembly assembly)
        {
            foreach (var dep in assembly.GetReferencedAssemblies())
            {
                if (!LoadedAssemblies.ContainsKey(dep))
                {
                    var loaded_dep = LoadAssembly(dep);
                    LoadedAssemblies.Add(loaded_dep.GetName(), loaded_dep);
                }
            }

            return assembly;
        }

        public string[] LoadAssemblyClasses(AssemblyName assembly)
        {
            return LoadAssembly(assembly).Subclasses(typeof(MarshalByRefObject)).ToArray();
        }
    }

    static class AssemblyExtensions
    {
        static public IEnumerable<string> Subclasses(this Assembly assembly, Type basetype)
        {
            return assembly.GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(basetype)).Select(c => c.Name);
        }
    }
}
