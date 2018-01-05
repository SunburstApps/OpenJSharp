using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JSharp.BuildTasks
{
    public sealed class JSharpStubGenerator : ToolTask
    {
        [Required]
        public ITaskItem ToolLocation { get; set; }
        [Required]
        public ITaskItem JarFile { get; set; }
        [Required]
        public ITaskItem[] InputAssemblies { get; set; }
        public string[] Namespaces { get; set; }

        protected override string ToolName => "jsharpstubgen";
        protected override string GenerateFullPathToTool() => ToolLocation.GetMetadata("FullPath");

        protected override string GenerateCommandLineCommands()
        {
            List<string> argv = new List<string>();

            argv.Add("-out:" + JarFile.GetMetadata("FullPath"));
            argv.AddRange(Namespaces.Select(x => "-namespace:" + x));
            argv.AddRange(InputAssemblies.Select(x => x.GetMetadata("FullPath")));

            return string.Join(" ", argv.Select(x => $"\"{x}\""));
        }
    }
}
