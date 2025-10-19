using System;

namespace HashLib7
{
    public class ReportStatus
    {
        public long fileCount;
        public long filesProcessed;
        public StateEnum state;
        public DateTime startTime;
        public DateTime timeRemaining;
        public int threadCount;
        public string outputFile;
    }
}
