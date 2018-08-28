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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;

namespace raztalk
{
    public class RemoteLoader : MarshalByRefObject
    {
        public string Folder { get; set; }

        public RemoteLoader()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
        }

        public Assembly LoadAssembly(string assemblyPath)
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        public Assembly AssemblyResolve(object o, ResolveEventArgs e)
        {
            string dll = new AssemblyName(e.Name).Name + ".dll";

            FileInfo file = new DirectoryInfo("/").GetFiles().First(f => f.Name.Equals(dll, StringComparison.InvariantCultureIgnoreCase));
            if (file != null)
                return Assembly.LoadFile(file.FullName);

            file = new DirectoryInfo(Folder).GetFiles().First(f => f.Name.Equals(dll, StringComparison.InvariantCultureIgnoreCase));
            if (file != null)
                return Assembly.LoadFile(file.FullName);

            return AppDomain.CurrentDomain.Load(e.Name);
        }
    }
}
