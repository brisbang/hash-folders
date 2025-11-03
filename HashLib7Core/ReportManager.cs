using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Graph.Groups.Item.Team.Channels.Item.FilesFolder;

namespace HashLib7
{
    public enum ReportTaskEnum
    {
        rteGetFiles,
        rteProcessFile,
        rteWaitingForFiles,
        rteNoMoreFiles,
    }
    public class ReportManager : IAsyncManager
    {
        private List<ReportWorker> _threads;
        private string _path;
        private string _outputFile;
        private long _numFilesProcessed;
        private long _numFilesCounted;
        private DateTime _startTime;
        private int _numThreadsRunning;
        private bool _gettingFiles;
        private Queue<FileInfo> _files;
        public StateEnum State { get; private set; }
        private static object _mutex = new();
        private static object _mutexFile = new();
        private static object _threadMutex = new();
        private bool _headerWritten;
        public ReportManager()
        {
            State = StateEnum.Stopped;
            _headerWritten = false;
        }

        public void ExecuteAsync(string path, int numThreads)
        {
            lock (_mutex)
            {
                if (_threads == null)
                {
                    _threads = [];
                    _path = path;
                    _gettingFiles = false;
                    _startTime = DateTime.Now;
                    _outputFile = String.Format("{0}\\Report-{1}.csv", Config.DataPath, _startTime.ToString("yyyy-MM-dd-HHmmss"));
                    State = StateEnum.Running;
                    for (int i = 0; i < numThreads; i++)
                    {
                        ReportWorker thread = new(this);
                        thread.ExecuteAsync();
                        _threads.Add(thread);
                    }
                }
            }
        }

        public void Abort()
        {
            if (this.State == StateEnum.Running || this.State == StateEnum.Suspended)
                this.State = StateEnum.Aborting;
        }

        public void Suspend()
        {
            if (this.State == StateEnum.Running)
                this.State = StateEnum.Suspended;
        }

        public void Resume()
        {
            if (this.State == StateEnum.Suspended)
                this.State = StateEnum.Running;
        }

        public ReportStatus GetStatus()
        {
            ReportStatus res = new()
            {
                startTime = _startTime,
                filesOutstanding = _numFilesCounted - _numFilesProcessed,
                filesProcessed = _numFilesProcessed,
                threadCount = _numThreadsRunning,
                state = State,
                duration = new TimeSpan(DateTime.Now.Ticks - _startTime.Ticks)
            };
            if (_numFilesProcessed > 0)
            {
                if (this.State == StateEnum.Running || this.State == StateEnum.Suspended)
                {
                    res.timeRemaining = DateTime.MinValue.AddTicks((long)((double)_numFilesCounted * (DateTime.Now.Ticks - res.startTime.Ticks) / _numFilesProcessed));
                }
            }
            res.outputFile = _outputFile;
            return res;
        }

        internal void ThreadIsStarted()
        {
            lock (_threadMutex)
            {
                ++_numThreadsRunning;
            }
        }

        internal void ThreadIsFinished()
        {
            lock (_threadMutex)
            {
                --_numThreadsRunning;
                Finalise();
            }
        }

        private void Finalise()
        {
            if (_numThreadsRunning > 0)
                return;
            Config.LogDebugging("State: Stopped");
            State = StateEnum.Stopped;
        }

        internal void LogLine(string line, bool reportFileProcessed)
        {
            lock (_mutexFile)
            {
                using System.IO.FileStream outputFileStream = System.IO.File.Open(_outputFile, System.IO.FileMode.Append);
                byte[] outputBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(line);
                outputFileStream.Write(outputBytes, 0, outputBytes.Length);
                Config.LogDebugging("Processed file");
                if (reportFileProcessed)
                   _numFilesProcessed++;
            }
        }

        internal ReportTaskEnum GetNextTask(out FileInfo file)
        {
            file = null;
            lock (_mutexFile)
            {
                if (_files == null)
                {
                    if (_gettingFiles)
                        return ReportTaskEnum.rteWaitingForFiles;
                    _gettingFiles = true;
                    return ReportTaskEnum.rteGetFiles;
                }
                if (_files.Count == 0)
                    return ReportTaskEnum.rteNoMoreFiles;
                file = _files.Dequeue();
                return ReportTaskEnum.rteProcessFile;
            }
        }

        internal void SetFiles(Queue<FileInfo> files)
        {
            lock (_mutexFile)
            {
                _files = files;
                _numFilesCounted = _files.Count;
                Config.LogDebugging("Found " + _numFilesCounted + " file(s)");
                _gettingFiles = false;
            }
        }

        internal void TryWriteHeader()
        {
            if (!_headerWritten)
            {
                lock (_mutexFile)
                {
                    if (!_headerWritten)
                    {
                        LogLine(ReportHeader(), false);
                        _headerWritten = true;
                    }
                }
            }
        }

        private static string ReportHeader()
        {
            string res = "Filename,Hash,Size,Local copies,Local backups,Remote backups,Local copy 1,Local backup 1,Remote backup 1\n";
            return res;
        }

        public string Path => _path;

    }
}
