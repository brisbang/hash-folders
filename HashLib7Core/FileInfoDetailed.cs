using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class FileInfoDetailed : FileInfo
    {
        public DateTime lastModified;
        public List<PathFormatted> BackupLocations;

        public FileInfoDetailed(string filePath) : base(filePath)
        {
        }

        public FileInfoDetailed(string path, string name) : base(path, name)
        {
        }
    }
}