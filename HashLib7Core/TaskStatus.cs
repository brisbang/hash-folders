using System;

namespace HashLib7
{
    public class TaskStatus
    {
        public StateEnum state;
        public long filesProcessed;
        public DateTime startTime;
        public int threadCount;
        public TimeSpan duration;
        public long filesOutstanding;
    }
}