using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib7
{
    internal class Io
    {
        public static string[] GetFiles(string folder)
        {
            return System.IO.Directory.GetFiles(folder);
        }

        public static string[] GetFolders(string folder)
        {
            return System.IO.Directory.GetDirectories(folder);
        }

        public static DateTime GetLastModified(string file)
        {
            return System.IO.File.GetLastWriteTime(file);
        }

        public static long GetLength(string file)
        {
            return new System.IO.FileInfo(file).Length;
        }

        public static System.IO.FileStream GetFileStream(string file)
        {
            return System.IO.File.OpenRead(file);
        }

    }
}
