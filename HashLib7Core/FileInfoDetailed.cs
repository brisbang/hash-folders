using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class FileInfoDetailed
    {
        public string path;
        public string filename;
        public long size;
        public string hash;
        public DateTime lastModified;
        public List<PathFormatted> backupLocations;
    }
}