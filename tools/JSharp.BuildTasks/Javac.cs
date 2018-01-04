using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;

namespace JSharp.BuildTasks
{
    public sealed class Javac : ToolTask
    {
        private enum ToolLocationStrategy
        {
            System,
            Bundled
        }

        [Required]
        public string ToolMode { get; set; }

        public string[] ExtraArguments { get; set; }
        public ITaskItem[] BootClassPath { get; set; }
        public ITaskItem[] ClassPath { get; set; }
        [Required]
        public ITaskItem[] SourceFiles { get; set; }
        [Required]
        public ITaskItem WorkingDirectory { get; set; }

        private ToolLocationStrategy m_LocationStrategy = ToolLocationStrategy.Bundled;
        private string m_responseFile;

        protected override string ToolName => "javac";

        protected override string GenerateFullPathToTool()
        {
            if (m_LocationStrategy == ToolLocationStrategy.Bundled)
            {
                string myPath = typeof(Javac).Assembly.Location;
                string directory = Path.GetDirectoryName(myPath);
                return Path.Combine(directory, "jsharp_javac");
            }
            else if (m_LocationStrategy == ToolLocationStrategy.System)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string javaRootPath = null;
                    using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    {
                        using (RegistryKey key = hklm.OpenSubKey(@"SOFTWARE\JavaSoft\Java Runtime Environment\1.8"))
                        {
                            javaRootPath = (string)key.GetValue("JavaHome", null);
                        }
                    }

                    if (javaRootPath == null)
                    {

                        using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                        {
                            using (RegistryKey key = hklm.OpenSubKey(@"SOFTWARE\JavaSoft\Java Runtime Environment\1.8"))
                            {
                                javaRootPath = (string)key.GetValue("JavaHome", null);
                            }
                        }
                    }

                    if (javaRootPath != null)
                    {
                        return Path.Combine(javaRootPath, "bin", "javac.exe");
                    }
                    else
                    {
                        Log.LogError("Could not find Java 1.8");
                        return null;
                    }
                }
                else
                {
                    return PathSearcher.GetFullPathForExecutable("javac");
                }
            }
            else
            {
                throw new InvalidOperationException("Unrecognized tool location strategy");
            }
        }

        protected override string GenerateCommandLineCommands()
        {
            // All arguments are in the response file.
            return "@" + m_responseFile;
        }

        protected override string GetWorkingDirectory()
        {
            return WorkingDirectory.GetMetadata("FullPath");
        }

        public override bool Execute()
        {
            bool parseOK = Enum.TryParse(ToolMode, true, out m_LocationStrategy);
            if (!parseOK)
            {
                Log.LogError("Invalid ToolMode {0}", ToolMode);
                return false;
            }

            m_responseFile = Path.GetTempFileName();
            List<string> argv = new List<string>();
            argv.AddRange(ExtraArguments ?? Enumerable.Empty<string>());
            if (BootClassPath != null && BootClassPath.Length > 0)
            {
                argv.Add("-bootclasspath");
                argv.Add(string.Join(Path.PathSeparator.ToString(), BootClassPath.Select(x => x.GetMetadata("FullPath"))));
            }
            if (ClassPath != null && ClassPath.Length > 0)
            {
                argv.Add("-cp");
                argv.Add(string.Join(Path.PathSeparator.ToString(), ClassPath.Select(x => x.GetMetadata("FullPath"))));
            }
            argv.AddRange(SourceFiles?.Select(x => x.GetMetadata("FullPath")) ?? Enumerable.Empty<string>());
            File.WriteAllLines(m_responseFile, argv);

            bool taskSuccess = base.Execute();

            if (taskSuccess)
            {
                DeleteTempFile(m_responseFile);
            }
            else
            {
                Log.LogMessage(MessageImportance.High, "Javac arguments can be found here: {0}", m_responseFile);
            }

            return taskSuccess;
        }
    }
}
