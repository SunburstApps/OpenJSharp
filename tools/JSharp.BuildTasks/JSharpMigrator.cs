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
        private enum ParsedOutputType
        {
            Exe,
            WinExe,
            Library
        }

        private enum ParsedPlatform
        {
            x86,
            x64,
            ARM,
            AnyCpuPrefer32Bit,
            AnyCpu
        }

        [Required]
        public ITaskItem ToolLocation { get; set; }
        [Required]
        public string OutputType { get; set; }
        [Required]
        public string Platform { get; set; }
        public bool CompressResources { get; set; }
        public bool StrictFieldSemantics { get; set; }
        public bool RemoveAssertions { get; set; }
        public bool SharedClassLoader { get; set; }
        public bool SuppressParameterReflection { get; set; }
        public string[] NoWarn { get; set; }
        public string[] ExtraArguments { get; set; }
        public string Version { get; set; }
        public string FileVersion { get; set; }
        public string ApplicationIcon { get; set; }
        public string Win32Manifest { get; set; }
        public ITaskItem[] References { get; set; }
        [Required]
        public ITaskItem OutputPath { get; set; }
        [Required]
        public ITaskItem[] ClassFiles { get; set; }

        protected override string ToolName => "jsharpmigrate";
        protected override string GenerateFullPathToTool() => ToolLocation.GetMetadata("FullPath");

        private string m_responseFile;
        private ParsedPlatform m_Platform;
        private ParsedOutputType m_OutputType;

        protected override string GenerateCommandLineCommands()
        {
            // All argumentsare in the response file.
            return "@" + m_responseFile;
        }

        public override bool Execute()
        {
            if (!Enum.TryParse(OutputType, true, out m_OutputType))
            {
                Log.LogError("Unrecognized OutputType {0}", OutputType);
                return false;
            }

            if (!Enum.TryParse(Platform, true, out m_Platform))
            {
                Log.LogError("Unrecognized Platform {0}", Platform);
                return false;
            }

            m_responseFile = Path.GetTempFileName();
            List<string> argv = new List<string>();

            argv.Add("-nologo");
            argv.Add("-version:1.8");
            argv.Add("-w4");

            switch (m_OutputType)
            {
                case ParsedOutputType.Exe:
                    argv.Add("-target:exe");
                    break;
                case ParsedOutputType.WinExe:
                    argv.Add("-target:winexe");
                    break;
                case ParsedOutputType.Library:
                    argv.Add("-target:library");
                    break;
            }

            switch (m_Platform)
            {
                case ParsedPlatform.AnyCpu:
                    argv.Add("-platform:anycpu");
                    break;
                case ParsedPlatform.AnyCpuPrefer32Bit:
                    argv.Add("-platform:anycpu32bitpreferred");
                    break;
                case ParsedPlatform.ARM:
                    argv.Add("-platform:arm");
                    break;
                case ParsedPlatform.x64:
                    argv.Add("-platform:x64");
                    break;
                case ParsedPlatform.x86:
                    argv.Add("-platform:x86");
                    break;
            }

            if (!string.IsNullOrEmpty(Version)) argv.Add("-version:" + Version);
            if (!string.IsNullOrEmpty(FileVersion)) argv.Add("-fileversion:" + FileVersion);
            if (!string.IsNullOrEmpty(ApplicationIcon)) argv.Add("-win32icon:" + ApplicationIcon);
            if (!string.IsNullOrEmpty(Win32Manifest)) argv.Add("-win32manifest:" + Win32Manifest);

            argv.AddRange(NoWarn.Select(x => "-nowarn:" + x));
            if (CompressResources) argv.Add("-compressresources");
            if (StrictFieldSemantics) argv.Add("-strictfinalfieldsemantics");
            if (RemoveAssertions) argv.Add("-removeassertions");
            if (SharedClassLoader) argv.Add("-sharedclassloader");
            if (SuppressParameterReflection) argv.Add("-noparameterreflection");
            argv.AddRange(ExtraArguments ?? Enumerable.Empty<string>());
            argv.Add("-highentropyva"); // for security

            argv.Add("-out:" + OutputPath.GetMetadata("FullPath"));
            argv.AddRange(ClassFiles?.Select(x => x.GetMetadata("FullPath")) ?? Enumerable.Empty<string>());

            File.WriteAllLines(m_responseFile, argv);
            bool taskSuccess = base.Execute();

            if (taskSuccess)
            {
                DeleteTempFile(m_responseFile);
            }
            else
            {
                Log.LogMessage(MessageImportance.High, "Migrator arguments can be found here: {0}", m_responseFile);
            }

            return taskSuccess;
        }
    }
}
