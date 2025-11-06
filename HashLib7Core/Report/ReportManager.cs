using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Graph.Groups.Item.Team.Channels.Item.FilesFolder;

namespace HashLib7
{
    public class ReportManager : AsyncManager
    {

        private string _outputFile;
        private object _mutexFile = new();

        protected override List<Worker> ExecuteInvoked(int numThreads)
        {
            _outputFile = String.Format("{0}\\Report-{1}.csv", Config.DataPath, StartTime.ToString("yyyy-MM-dd-HHmmss"));
            List<Worker> threads = [];
            for (int i = 0; i < numThreads; i++)
                threads.Add(new ReportWorker(this));
            return threads;
        }

        public ReportStatus GetStatus()
        {
            ReportStatus res = new()
            {
                startTime = StartTime,
                threadCount = NumThreadsRunning,
                state = State,
                duration = Duration,
                outputFile = _outputFile,
                filesOutstanding = NumFilesOutstanding,
                filesProcessed = NumFilesProcessed,
            };
            return res;
        }

        protected override void InitialiseInvoked()
        {
            LogLine(ReportHeader());
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
        private static string ReportHeader()
        {
            string res = "Filename,Hash,Size,Local copies,Local backups,Remote backups,Local copy 1,Local backup 1,Remote backup 1\n";
            return res;
        }

    }
}
