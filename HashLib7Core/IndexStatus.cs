using System;

namespace HashLib7
{
    public class IndexStatus
    {
        public long filesProcessed;
        public long filesToDelete;
        public long foldersProcessed;
        public long foldersOutstanding;
        public long filesOutstanding;
        public StateEnum state;
        public DateTime startTime;
        public DateTime timeRemaining;
        public int threadCount;
        public string outputFile;
        public TimeSpan duration;
        public IndexPhaseEnum phase;
    }

    public enum IndexPhaseEnum

    {
        Scanning,
        Cleaning,
        Done
    }
}
