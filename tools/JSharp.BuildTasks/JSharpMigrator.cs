using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JSharp.BuildTasks
{
    public class JSharpMigrator : ToolTask
    {
        [Required]
        public ITaskItem ToolLocation { get; set; }
        public bool CompressResources { get; set; }
        public bool StrictFieldSemantics { get; set; }
        public bool RemoveAssertions { get; set; }
        public bool SharedClassLoader { get; set; }
        public bool SuppressParameterReflection { get; set; }
        public string[] NoWarn { get; set; }
        public string[] ExtraArguments { get; set; }
        public ITaskItem[] References { get; set; }
        [Required]
        public ITaskItem[] SourceFiles { get; set; }

        protected override string ToolName => "jsharpmigrate";
        protected override string GenerateFullPathToTool() => ToolLocation.GetMetadata("FullPath");

        private string m_responseFile;

        protected override string GenerateCommandLineCommands()
        {
            // All argumentsare in the response file.
            return "@" + m_responseFile;
        }

        public override bool Execute()
        {
            m_responseFile = Path.GetTempFileName();
            List<string> argv = new List<string>();

            argv.Add("-version:1.8");
            argv.Add("-w4");
            argv.AddRange(NoWarn.Select(x => "-nowarn:" + x));
            if (CompressResources) argv.Add("-compressresources");
            if (StrictFieldSemantics) argv.Add("-strictfinalfieldsemantics");
            if (RemoveAssertions) argv.Add("-removeassertions");
            if (SharedClassLoader) argv.Add("-sharedclassloader");
            if (SuppressParameterReflection) argv.Add("-noparameterreflection");
            argv.AddRange(ExtraArguments ?? Enumerable.Empty<string>());
            argv.AddRange(SourceFiles.Select(x => x.GetMetadata("FullPath")));

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
