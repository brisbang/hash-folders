using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private List<char> _drives;
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
                    _startTime = DateTime.Now;
                    _outputFile = String.Format("{0}\\Report-{1}.csv", Config.DataPath, _startTime.ToString("yyyy-MM-dd-HHmmss"));
                    State = StateEnum.Running;
                    for (int i = 0; i < numThreads; i++)
                    {
                        System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ThreadStart(this.ExecuteInternal));
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
            Database d = new(Config.DatabaseFile);
            PathFormatted p = new(_path);
            if (_files == null)
            {
                lock (_mutexList)
                {
                    if (_files == null)
                    {
                        _files = d.GetFilesByPath(_path);
                        _numFilesCounted = _files.Count;
                        _drives = d.GetDrives();
                        LogLine(ReportHeader(_drives));
                    }
                }
            }
            try
            {
                List<FileDrive> drives = new();
                foreach (char drive in _drives)
                    drives.Add(new FileDrive() { Letter = drive });
                while (_files.Count > 0)
                {
                    ExtractAndProcessLine(d, drives);
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
                using (System.IO.FileStream outputFileStream = System.IO.File.Open(_outputFile, System.IO.FileMode.Append))
                {
                    byte[] outputBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(line);
                    outputFileStream.Write(outputBytes, 0, outputBytes.Length);
                }
            }
        }

        private FileInfo ExtractAndProcessLine(Database d, List<FileDrive> drives)
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
                ReportFile(d, drives, file);
                lock (_mutexProcessed)
                {
                    _numFilesProcessed++;
                }
            }

            return file;
        }

        private void ReportFile(Database d, List<FileDrive> drives, FileInfo file)
        {
            ReportRow rr = new()
            {
                hash = file.hash,
                filename = file.filename,
                size = file.size
            };
            if (file.size > 0)
            {
                List<string> matchingFiles = d.GetFilesByHash(file.hash);
                foreach (FileDrive drive in drives)
                {
                    drive.files = new();
                }
                foreach (string match in matchingFiles)
                    ReviewMatchingFiles(drives, file, rr, match);
            }
            LogLine(ReportRowToString(drives, rr));
        }

        private static void ReviewMatchingFiles(List<FileDrive> drives, FileInfo file, ReportRow rr, string match)
        {
            if (match.Length == 0) throw new ArgumentNullException("NULL returned when searching for hash " + file.hash);
            //Don't report the same file
            if (match == file.filename) return;
            foreach (FileDrive drive in drives)
            {
                if (match[0] == drive.Letter)
                {
                    drive.files.Add(match);
                    return;
                }
            }
        }

        private string ReportHeader(List<char> drives)
        {
            string res = "Filename,Hash,Size,Size on the same drive";
            foreach (char drive in drives)
            {
                res += ",Duplicates on " + drive;
            }
            foreach (char drive in drives)
            {
                res += ",First duplicate on " + drive;
            }
            res += ",Local duplicate 1";
            res += ",Local duplicate 2";
            res += ",Local duplicate 3";
            res += ",Local duplicate 4";
            res += "\n";
            return res;
        }

        private string ReportRowToString(List<FileDrive> drives, ReportRow rr)
        {
            long sizeDuplicated = 0;
            string res = String.Format("{0},{1},{2}", SafeFilename(rr.filename), rr.hash, rr.size.ToString());
            List<string> otherCopies = null;
            foreach (FileDrive drive in drives)
            {
                if (rr.filename[0] == drive.Letter)
                {
                    sizeDuplicated = rr.size * drive.files.Count;
                    otherCopies = drive.files;
                    break;
                }
            }
            res += String.Format(",{0}", sizeDuplicated.ToString());
            foreach (FileDrive drive in drives)
                res += String.Format(",{0}", drive.files.Count);
            foreach (FileDrive drive in drives)
            {
                if (0 == drive.files.Count)
                    res += ",";
                else
                    res += String.Format(",{0}", SafeFilename(drive.files[0]));
            }
            if (otherCopies != null)
            {
                for (int i = 0; i < Math.Min(otherCopies.Count, 4); i++)
                    res += String.Format(",{0}", SafeFilename(otherCopies[i]));
            }
            res += '\n';
            return res;
        }

        private string SafeFilename(string filename)
        {
            return filename.Replace(',', '|');
        }
    }
}
