using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class ReportManager(string path, int numThreadsDesired) : AsyncManager(path, numThreadsDesired)
    {

        internal string OutputFile { get; set; }
        private readonly object MutexFile = new();

        public override Task GetFileTask(FileInfo file)
        {
            return new ReportTaskFile(this, file);
        }

        public override Task GetFolderTask(string folder)
        {
            return new ReportTaskFolder(this, folder);
        }

        public override ManagerStatus GetStatus()
        {
            ReportManagerStatus res = new()
            {
                startTime = StartTime,
                threadCount = NumThreadsRunning,
                state = State,
                duration = Duration,
                outputFile = OutputFile,
                filesOutstanding = NumFilesOutstanding,
                filesProcessed = NumFilesProcessed,
                workerStatuses = base.GetWorkerStatuses(),
            };
            return res;
        }

        protected override Task GetInitialTask()
        {
            return new ReportHeaderTask(this);
        }

        internal void LogDetail(string line)
        {
            LogLine(line);
        }

        private void LogLine(string line)
        {
            lock (MutexFile)
            {
                using System.IO.FileStream outputFileStream = System.IO.File.Open(OutputFile, System.IO.FileMode.Append);
                byte[] outputBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(line);
                outputFileStream.Write(outputBytes, 0, outputBytes.Length);
            }
        }
    }
}
