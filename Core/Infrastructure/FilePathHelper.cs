namespace RealmPeek.Core.Infrastructure
{
    /// <summary>
    /// Handles file path resolution relative to project root
    /// </summary>
    public static class FilePathHelper
    {
        private static readonly string BaseDirectory =
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));

        public static string GetFullPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(BaseDirectory, relativePath));
        }

        public static bool FileExists(string relativePath)
        {
            return File.Exists(GetFullPath(relativePath));
        }

        public static void EnsureDirectoryExists(string relativePath)
        {
            string fullPath = GetFullPath(relativePath);
            Directory.CreateDirectory(fullPath);
        }

        public static string[] ReadAllLines(string relativePath)
        {
            string fullPath = GetFullPath(relativePath);
            return File.Exists(fullPath) ? File.ReadAllLines(fullPath) : Array.Empty<string>();
        }

        public static void WriteAllLines(string relativePath, System.Collections.Generic.IEnumerable<string> lines)
        {
            string fullPath = GetFullPath(relativePath);
            EnsureDirectoryExists(Path.GetDirectoryName(relativePath) ?? "");
            File.WriteAllLines(fullPath, lines);
        }
    }
}
