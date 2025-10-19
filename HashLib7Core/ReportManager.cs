using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace HashLib7
{
    public class ReportManager
    {
        private List<System.Threading.Thread> _threads;
        private string _path;
        private string _outputFile;
        private long _numFilesProcessed;
        private long _numFilesCounted;
        private DateTime _startTime;
        private int _numThreadsRunning;
        private Queue<FileInfo> _files;
        public StateEnum State { get; private set; }
        private static object _mutex = new();
        private static object _mutexList = new();
        private static object _mutexProcessed = new();
        private static object _mutexFile = new();
        public ReportManager()
        {
            State = StateEnum.Stopped;
        }

        public void ExecuteAsync(string path, int numThreads)
        {
            lock (_mutex)
            {
                if (_threads == null)
                {
                    _threads = new();
                    _path = path;
                    UserSettings.RecentlyUsedFolder = path;
                    UserSettings.ReportThreadCount = numThreads;
                    _startTime = DateTime.Now;
                    _outputFile = String.Format("{0}\\Report-{1}.csv", Config.DataPath, _startTime.ToString("yyyy-MM-dd-HHmmss"));
                    State = StateEnum.Running;
                    for (int i = 0; i < numThreads; i++)
                    {
                        System.Threading.Thread thread = new(new System.Threading.ThreadStart(this.ExecuteInternal));
                        thread.Start();
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
                fileCount = _numFilesCounted,
                filesProcessed = _numFilesProcessed,
                threadCount = _numThreadsRunning,
                state = State
            };
            if (_numFilesProcessed > 0)
            {
                if (this.State == StateEnum.Running || this.State == StateEnum.Suspended)
                {
                    res.timeRemaining = res.startTime.AddTicks((long)((double)_numFilesCounted * (DateTime.Now.Ticks - res.startTime.Ticks) / _numFilesProcessed));
                }
            }
            res.outputFile = _outputFile;
            return res;
        }

        private void ExecuteInternal()
        {
            _numThreadsRunning++;
            Database d = Config.GetDatabase();
            PathFormatted p = new(_path);
            if (_files == null)
            {
                lock (_mutexList)
                {
                    if (_files == null)
                    {
                        _files = d.GetFilesByPath(_path);
                        _numFilesCounted = _files.Count;
                        LogLine(ReportHeader());
                    }
                }
            }
            try
            {
                while (_files.Count > 0)
                {
                    ExtractAndProcessLine(d);
                    switch (this.State)
                    {
                        case StateEnum.Stopped: return; //Weird
                        case StateEnum.Aborting: return;
                        case StateEnum.Suspended:
                            while (this.State == StateEnum.Suspended)
                                System.Threading.Thread.Sleep(500);
                            break;
                        case StateEnum.Running:
                            break;
                    }
                }
            }
            finally {
                _numThreadsRunning--;
                if (_numThreadsRunning == 0) 
                    this.State = StateEnum.Stopped;
            }
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

        private FileInfo ExtractAndProcessLine(Database d)
        {
            FileInfo file;
            lock (_mutexList)
            {
                file = null;
                if (_files.Count > 0)
                    file = _files.Dequeue();
            }
            if (file != null)
            {
                ReportFile(d, file);
                lock (_mutexProcessed)
                {
                    _numFilesProcessed++;
                }
            }

            return file;
        }

        private void ReportFile(Database d, FileInfo file)
        {
            ReportRow rr = new()
            {
                hash = file.hash,
                filePath = file.filePath,
                size = file.size
            };
            FileLocations locations = new(file.filePath);
            if (file.size > 0)
            {
                List<PathFormatted> matchingFiles = d.GetFilesByHash(file.hash);
                foreach (PathFormatted match in matchingFiles)
                    locations.AddDuplicate(match);
            }
            LogLine(ReportRowToString(locations, rr));
        }

        private static string ReportHeader()
        {
            string res = "Filename,Hash,Size,Local copies,Local backups,Remote backups,Local copy 1,Local backup 1,Remote backup 1\n";
            return res;
        }

        private string ReportRowToString(FileLocations locations, ReportRow rr)
        {
            List<string> localCopies = locations.Copies(LocationEnum.LocalCopy);
            List<string> localBackups = locations.Copies(LocationEnum.LocalBackup);
            List<string> remoteBackups = locations.Copies(LocationEnum.RemoteBackup);
            string localCopyFirst = String.Empty;
            string localBackupFirst = String.Empty;
            string remoteBackupFirst = String.Empty;
            if (localCopies.Count > 0) localCopyFirst = SafeFilename(localCopies[0]);
            if (localBackups.Count > 0) localBackupFirst = SafeFilename(localBackups[0]);
            if (remoteBackups.Count > 0) remoteBackupFirst = SafeFilename(remoteBackups[0]);

            string res = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}\n", SafeFilename(rr.filePath), rr.hash, rr.size.ToString(), 
               localCopies.Count.ToString(),
               localBackups.Count.ToString(),
               remoteBackups.Count.ToString(),
               localCopyFirst,
               localBackupFirst,
               remoteBackupFirst);
            return res;
        }

        private static string SafeFilename(string filename)
        {
            return filename.Replace(',', '|');
        }
    }
}
