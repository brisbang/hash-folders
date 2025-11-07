using System;

namespace HashLib7
{
    public class IndexManagerStatus : ManagerStatus
    {
        public long filesToDelete;
        public long foldersProcessed;
        public long foldersOutstanding;
        public string outputFile;
        public IndexPhaseEnum phase;
    }

    public enum IndexPhaseEnum

    {
        Scanning,
        Cleaning,
        Done
    }
}
