using System;
using System.IO;

namespace JSharp.BuildTasks
{
    internal static class PathSearcher
    {
        internal static string GetFullPathForExecutable(string executableName)
        {
            string[] directories = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator);

            foreach (string dir in directories)
            {
                string path = Path.Combine(dir, executableName);
                if (File.Exists(path)) return path;
            }

            return null;
        }
    }
}
