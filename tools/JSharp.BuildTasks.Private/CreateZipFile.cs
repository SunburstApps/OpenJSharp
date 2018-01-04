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
            string outputPath = ZipFile.GetMetadata("FullPath");
            using (ZipArchive archive = ZipFile.Open(outputPath, ZipArchiveMode.Create))
            {
                foreach (ITaskItem item in Entries)
                {
                    string logicalName = item.GetMetadata("EntryName");
                    if (string.IsNullOrEmpty(logicalName)) logicalName = item.ItemSpec;

                    archive.CreateEntryFromFile(item.GetMetadata("FullPath"), logicalName);
                }
            }

            return !Log.HasLoggedErrors;
        }
    }
}
