/*
  Copyright (C) 2013 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net

*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
using Type = IKVM.Reflection.Type;

namespace IKVM.BuildTasks.Private
{
    public sealed class ImportLibraryGenerator : Task
    {
        private static readonly Regex definition = new Regex(@"^\s*(.+)=\[([^\]]+)\](.+)::([^\s]+)\s+@(\d+)$");

        private struct Export
        {
            internal string name;
            internal int ordinal;
            internal MethodInfo method;
        }

        private Universe universe = new Universe();

        [Required]
        public ITaskItem DefFile { get; set; }

        [Required]
        public ITaskItem OutputFile { get; set; }

        [Required]
        public string Platform { get; set; }

        public ITaskItem[] References { get; set; }
        public ITaskItem Win32Resource { get; set; }
        public string Version { get; set; }
        public string Product { get; set; }
        public string Company { get; set; }
        public string Copyright { get; set; }
        public string FileDescription { get; set; }

        public override bool Execute()
        {
            PortableExecutableKinds peKind;
            ImageFileMachine machine;

            if (Platform.Equals("X86", StringComparison.OrdinalIgnoreCase))
            {
                peKind = PortableExecutableKinds.Required32Bit;
                machine = ImageFileMachine.I386;
            }
            else if (Platform.Equals("amd64", StringComparison.OrdinalIgnoreCase))
            {
                peKind = PortableExecutableKinds.PE32Plus;
                machine = ImageFileMachine.AMD64;
            }
            else if (Platform.Equals("arm", StringComparison.OrdinalIgnoreCase))
            {
                peKind = PortableExecutableKinds.Unmanaged32Bit;
                machine = ImageFileMachine.ARM;
            }
            else
            {
                Log.LogError("Unrecognized Platform value: {0}", Platform);
                return false;
            }

            foreach (var asm in References)
            {
                universe.LoadFile(asm.GetMetadata("FullPath"));
            }

            List<Export> exports = new List<Export>();
            bool defFileOK = ParseDefFile(DefFile, exports);
            if (!defFileOK) return false;
            
            AssemblyName name = new AssemblyName(OutputFile.GetMetadata("FileName"));
            name.Version = new Version(Version);

            AssemblyBuilder ab = universe.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);
            ModuleBuilder modb = ab.DefineDynamicModule(name.Name, OutputFile.GetMetadata("FullPath"));

            foreach (Export exp in exports) ExportMethod(modb, exp);
            modb.CreateGlobalFunctions();

            if (Win32Resource != null)
            {
                ab.DefineUnmanagedResource(Win32Resource.GetMetadata("FullPath"));
            }
            else
            {
                if (FileDescription != null)
                {
                    var ctor = universe.Import(typeof(System.Reflection.AssemblyTitleAttribute)).GetConstructor(new Type[] { universe.Import(typeof(string)) });
                    ab.SetCustomAttribute(new CustomAttributeBuilder(ctor, new object[] { FileDescription }));
                }

                ab.DefineVersionInfoResource(Product, Version.ToString(), Company, Copyright, null);
            }

            ab.Save(OutputFile.GetMetadata("FullPath"), peKind, machine);
            Log.LogMessage("{0} -> {1}", DefFile.ItemSpec, OutputFile.ItemSpec);
            return true;
        }

        private bool ParseDefFile(ITaskItem file, List<Export> exports)
        {
            using (StreamReader reader = new StreamReader(file.GetMetadata("FullPath")))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Match m = definition.Match(line);
                    if (m.Groups.Count == 6)
                    {
                        Export exp;
                        exp.name = m.Groups[1].Value;
                        exp.ordinal = int.Parse(m.Groups[5].Value);
                        exp.method = FindMethod(m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value);
                        if (exp.method == null)
                        {
                            Log.LogError("Could not find method {0}", exp.name);
                            return false;
                        }
                    }
                    else
                    {
                        Log.LogWarning("malformed .def file line \"{0}\", ignoring", line);
                    }
                }
            }

            return true;
        }

        private MethodInfo FindMethod(string assemblyName, string typeName, string method)
        {
            foreach (Assembly asm in universe.GetAssemblies())
            {
                if (asm.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    Type type = asm.GetType(typeName);
                    if (type != null) return type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
                }
            }

            return null;
        }

        private void ExportMethod(ModuleBuilder module, Export exp)
        {
            ParameterInfo[] parameters = exp.method.GetParameters();
            Type[] types = new Type[parameters.Length];
            for (int i = 0; i < types.Length; i++) types[i] = parameters[i].ParameterType;

            MethodBuilder method = module.DefineGlobalMethod(exp.name, MethodAttributes.Public | MethodAttributes.Static, exp.method.ReturnType, types);
            ILGenerator il = method.GetILGenerator();
            for (int i = 0; i < types.Length; i++) il.Emit(OpCodes.Ldarg_S, (byte)i);
            il.Emit(OpCodes.Call, exp.method);
            il.Emit(OpCodes.Ret);

            method.__AddUnmanagedExport(method.Name, exp.ordinal);
        }
    }
}
