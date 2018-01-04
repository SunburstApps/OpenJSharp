using System.IO.Compression;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JSharp.BuildTasks.Private
{
    public sealed class CreateZipFile : Task
    {
        [Required]
        public ITaskItem OutputZipFile { get; set; }

        [Required]
        public ITaskItem[] Contents { get; set; }

        public override bool Execute()
        {
            string outputPath = OutputZipFile.GetMetadata("FullPath");
            using (ZipArchive archive = ZipFile.Open(outputPath, ZipArchiveMode.Create))
            {
                foreach (ITaskItem item in Contents)
                {
                    string logicalName = item.GetMetadata("NameInZip");
                    if (string.IsNullOrEmpty(logicalName)) logicalName = item.ItemSpec;

                    archive.CreateEntryFromFile(item.GetMetadata("FullPath"), logicalName);
                }
            }

            return !Log.HasLoggedErrors;
        }
    }
}
