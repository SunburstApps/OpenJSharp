using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace JSharp.BuildTasks
{
    internal static class PathSearcher
    {
        internal static string GetFullPathForExecutable(string executableName)
        {
            string[] directories = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator);

            foreach (string dir in directories)
            {
                string path = Path.Combine(dir, executableName);
                if (File.Exists(path)) return path;
            }

            return null;
        }

        internal static string ReadJavaRootPathFromRegistry()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException();
            }

            string javaRootPath = null;
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey key = hklm.OpenSubKey(@"SOFTWARE\JavaSoft\Java Development Kit\1.8"))
                {
                    javaRootPath = (string)key.GetValue("JavaHome", null);
                }
            }

            if (javaRootPath == null)
            {

                using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (RegistryKey key = hklm.OpenSubKey(@"SOFTWARE\JavaSoft\Java Development Kit\1.8"))
                    {
                        javaRootPath = (string)key.GetValue("JavaHome", null);
                    }
                }
            }

            return javaRootPath;
        }
    }

    internal enum ToolLocationStrategy
    {
        System,
        Bundled
    }
}
