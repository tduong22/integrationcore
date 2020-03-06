using System;
using System.IO;

namespace Comvita.Common.Actor.Utilities
{
    public class FileUtility
    {
        public static bool IsJsonExtension(string path)
        {
            return Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsXmlExtension(string path)
        {
            return Path.GetExtension(path).Equals(".xml", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsCsvExtension(string path)
        {
            return Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsTextExtension(string path)
        {
            return Path.GetExtension(path).Equals(".txt", StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsExpiditorExtension(string path)
        {
            return Path.GetExtension(path).Equals(".856", StringComparison.OrdinalIgnoreCase);
        }
    }
}
