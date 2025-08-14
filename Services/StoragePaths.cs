using System;
using System.IO;

namespace FastScreeny.Services
{
    public static class StoragePaths
    {
        public static string GetDefaultSaveDirectory()
        {
            var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            var dir = Path.Combine(pictures, "FastScreeny");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static string EnsureDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) path = GetDefaultSaveDirectory();
            Directory.CreateDirectory(path);
            return path;
        }
    }
}


