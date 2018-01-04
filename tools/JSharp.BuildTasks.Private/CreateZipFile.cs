using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JSharp.BuildTasks.Private
{
    public sealed class CreateZipFile : Task
    {
        [Required]
        public ITaskItem ZipFile { get; set; }

        [Required]
        public ITaskItem[] Entries { get; set; }

        public override bool Execute()
        {
            try
            {
                string outputPath = ZipFile.GetMetadata("FullPath");
                if (File.Exists(outputPath)) File.Delete(outputPath);

                using (ZipArchive archive = System.IO.Compression.ZipFile.Open(outputPath, ZipArchiveMode.Create))
                {
                    foreach (ITaskItem item in Entries)
                    {
                        string logicalName = item.GetMetadata("EntryName");
                        if (string.IsNullOrEmpty(logicalName)) logicalName = item.ItemSpec;

                        logicalName = Path.Combine(logicalName.Split(
                            new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries));
                        archive.CreateEntryFromFile(item.GetMetadata("FullPath"), logicalName);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Log.LogError("Could not find input file {0}", ex.FileName);
            }

            return !Log.HasLoggedErrors;
        }
    }
}
