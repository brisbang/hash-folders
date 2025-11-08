using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class ReportManager : AsyncManager
    {

        private string _outputFile;
        private object _mutexFile = new();
        internal bool HasCompletedHeader = false;

        public ReportManager()
        {
            _outputFile = String.Format("{0}\\Report-{1}.csv", Config.DataPath, StartTime.ToString("yyyy-MM-dd-HHmmss"));
        }

        public override Task GetFileTask(FileInfo file)
        {
            if (!HasCompletedHeader)
                return new TaskWait(this);
            return new ReportTaskFile(this, file);
        }

        public override Task GetFolderTask(string folder)
        {
            if (!HasCompletedHeader)
                return new TaskWait(this);
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
                outputFile = _outputFile,
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
            lock (_mutexFile)
            {
                using System.IO.FileStream outputFileStream = System.IO.File.Open(_outputFile, System.IO.FileMode.Append);
                byte[] outputBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(line);
                outputFileStream.Write(outputBytes, 0, outputBytes.Length);
            }
        }
    }
}
