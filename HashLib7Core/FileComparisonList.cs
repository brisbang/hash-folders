using System.Collections.Generic;

namespace HashLib7
{
    public class FolderCount
    {
        public string Folder { get; set; }
        public int Count { get; set; }
    }

    public class FileComparisonList
    {
        public List<FileInfoDetailed> Files { get; internal set; }
        public List<FolderCount> FolderCounts { get; internal set; }
        
        public FileComparisonList()
        {
            Files = [];
            FolderCounts = [];
        }
    }
}