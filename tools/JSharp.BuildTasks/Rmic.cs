using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JSharp.BuildTasks
{
    public sealed class Rmic : ToolTask
    {
        [Required]
        public string ToolMode { get; set; }
        [Required]
        public ITaskItem[] BootClassPath { get; set; }
        [Required]
        public ITaskItem[] ClassPath { get; set; }
        [Required]
        public ITaskItem OutputDirectory { get; set; }
        [Required]
        public string[] ClassNames { get; set; }
        public string[] ExtraArguments { get; set; }

        protected override string ToolName => "rmic";
        private ToolLocationStrategy m_LocationStrategy;
        private string m_responseFile;

        protected override string GenerateFullPathToTool()
        {
            if (m_LocationStrategy == ToolLocationStrategy.Bundled)
            {
                string myPath = typeof(Javac).Assembly.Location;
                string directory = Path.GetDirectoryName(myPath);
                return Path.Combine(directory, "jsharp_rmic");
            }
            else if (m_LocationStrategy == ToolLocationStrategy.System)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string javaRootPath = PathSearcher.ReadJavaRootPathFromRegistry();
                    if (javaRootPath != null)
                    {
                        return Path.Combine(javaRootPath, "bin", "rmic.exe");
                    }
                    else
                    {
                        Log.LogError("Could not find Java 1.8");
                        return null;
                    }
                }
                else
                {
                    return PathSearcher.GetFullPathForExecutable("rmic");
                }
            }
            else
            {
                throw new InvalidOperationException("Unrecognized tool location strategy");
            }
        }

        public override bool Execute()
        {
            if (!Enum.TryParse(ToolMode, true, out m_LocationStrategy))
            {
                Log.LogError("Invalid ToolMode {0}", ToolMode);
                return false;
            }

            m_responseFile = Path.GetTempFileName();
            List<string> argv = new List<string>();

            if (BootClassPath != null && BootClassPath.Length > 0)
            {
                argv.Add("-bootclasspath");
                argv.Add(string.Join(Path.PathSeparator.ToString(), BootClassPath.Select(x => x.GetMetadata("FullPath"))));
            }

            if (ClassPath != null && ClassPath.Length > 0)
            {
                argv.Add("-classpath");
                argv.Add(string.Join(Path.PathSeparator.ToString(), ClassPath.Select(x => x.GetMetadata("FullPath"))));
            }

            if (OutputDirectory != null)
            {
                argv.Add("-d");
                argv.Add(OutputDirectory.GetMetadata("FullPath"));
            }

            argv.AddRange(ExtraArguments);
            argv.AddRange(ClassNames);

            File.WriteAllLines(m_responseFile, argv);
            bool taskSuccess = base.Execute();

            if (taskSuccess)
            {
                DeleteTempFile(m_responseFile);
            }
            else
            {
                Log.LogMessage(MessageImportance.High, "Rmic arguments can be found here: {0}", m_responseFile);
            }

            return taskSuccess;
        }
    }
}
