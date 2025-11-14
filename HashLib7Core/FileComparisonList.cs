using System.Collections.Generic;

namespace HashLib7
{
    public class FolderCount
    {
        public string Folder { get; set; }
        public int Count { get; set; }
    }

    public class FileInfoDetailedComparison(FileInfoDetailed fileInfo)
    {
        public bool HasGeneralMatch { get; internal set; }
        public bool HasSpecificMatch { get; set; }
        public FileInfoDetailed FileInfo { get; private set; } = fileInfo;
    }

    public class FileComparisonList
    {
        public List<FileInfoDetailedComparison> Files { get; internal set; }
        public List<FolderCount> FolderCounts { get; internal set; }

        public FileComparisonList()
        {
            Files = [];
            FolderCounts = [];
        }
    }
}