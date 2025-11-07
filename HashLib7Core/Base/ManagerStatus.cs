using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class ManagerStatus
    {
        public StateEnum state;
        public long filesProcessed;
        public DateTime startTime;
        public int threadCount;
        public TimeSpan duration;
        public long filesOutstanding;
        public List<WorkerStatus> workerStatuses;
    }
}