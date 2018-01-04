using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JSharp.BuildTasks.Private
{
    public sealed class BootstrapStubGenerator : ToolTask
    {
        [Required]
        public string InputAssemblyName { get; set; }

        [Required]
        public ITaskItem OutputFile { get; set; }

        [Required]
        public string ToolLocation { get; set; }

        protected override string ToolName => "jsharpstubgen";
        protected override string GenerateFullPathToTool() => ToolLocation;

        protected override string GenerateCommandLineCommands()
        {
            List<string> argv = new List<string>();
            argv.Add("-bootstrap");
            argv.Add("-out:" + OutputFile.GetMetadata("FullPath"));
            argv.Add(InputAssemblyName);
            return string.Join(" ", argv.Select(x => $"\"{x}\""));
        }
    }
}
