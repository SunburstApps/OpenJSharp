using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JSharp.BuildTasks.Private
{
    public sealed class ReplaceToken : Task
    {
        [Required]
        public ITaskItem InputFile { get; set; }
        [Required]
        public ITaskItem OutputFile { get; set; }
        [Required]
        public string[] Replacements { get; set; }

        public override bool Execute()
        {
            try
            {
                string text = File.ReadAllText(InputFile.GetMetadata("FullPath"));

                foreach (string replacement in Replacements)
                {
                    string[] parts = replacement.Split('=');
                    if (parts.Length != 2)
                    {
                        Log.LogError("Malformed replacement '{0}': Expected exactly one equals sign", replacement);
                        continue;
                    }

                    text = text.Replace(parts[0], parts[1]);
                }

                if (!Log.HasLoggedErrors) File.WriteAllText(OutputFile.GetMetadata("FullPath"), text);
            }
            catch (FileNotFoundException ex)
            {
                Log.LogError("File not found: {0}", ex.FileName);
            }

            return !Log.HasLoggedErrors;
        }
    }
}
