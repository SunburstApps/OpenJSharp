using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace IKVM.BuildTasks.Private
{
    public sealed class SourceLicenseAnalyzer : Task
    {
        [Required]
        public ITaskItem[] SourceFiles { get; set; }

        public override bool Execute()
        {
            foreach (ITaskItem item in SourceFiles)
            {
                if (item.GetMetadata("FileName") == "AssemblyInfo") continue;
                ProcessFile(item);
            }

            return !Log.HasLoggedErrors;
        }

        void ProcessFile(ITaskItem item)
        {
            bool gpl = false, classPathException = false;
            string path = item.GetMetadata("FullPath");
            if (!File.Exists(path) && File.Exists(path + ".in"))
                path += ".in";

            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    gpl |= line.Contains("GNU General Public License");
                    classPathException |= line.Contains("subject to the \"Classpath\" exception");
                    classPathException |= line.Contains("permission to link this library with independent modules");
                }
            }

            if (gpl && !classPathException)
            {
                Log.LogError("GPL without Classpath exception detected on file {0}", item.ItemSpec);
            }
        }
    }
}
