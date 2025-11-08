using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class RAManager(string path, int numThreadsDesired) : AsyncManager(path, numThreadsDesired)
    {

        public override Task GetFileTask(FileInfo file)
        {
            return new RATaskFile(this, file);
        }

        public override Task GetFolderTask(string folder)
        {
            return new RATaskFolder(this, folder);
        }

        public override ManagerStatus GetStatus()
        {
            ReportManagerStatus res = new()
            {
                startTime = StartTime,
                threadCount = NumThreadsRunning,
                state = State,
                duration = Duration,
                filesOutstanding = NumFilesOutstanding,
                filesProcessed = NumFilesProcessed,
                workerStatuses = base.GetWorkerStatuses(),
            };
            return res;
        }

    }
}
