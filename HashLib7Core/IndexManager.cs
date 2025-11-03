using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class IndexManager : IAsyncManager
    {
        internal string Folder;
        private List<string> _folders;
        private int _folderIndex;
        private List<string> _files;
        private int _fileIndex;
        private List<IndexWorker> _threads;
        private DateTime _startTime;
        private int _numThreadsRunning;
        private object _fileMutex;
        private object _taskMutex;
        private object _threadMutex;
        private int _numThreads;
        private int _numFilesProcessed;
        private int _numFoldersProcessed;
        //private Credentials _credentials;
        public StateEnum State { get; private set; }
        //All previously known files under the folder. After processing is complete, any files in this list were not found and so can be removed from the database.
        //The short is not needed and so left 0 for performance reasons
        private SortedList<string, string> _previouslyRecordedFiles;

        //GetStatistics can be out slightly due to work in progress.
        //If a thread is processing the last file then that information is not shown because the work queue is empty but NumThreadsRunning is not yet zero.
        public IndexStatus GetStatistics()
        {
            IndexStatus res = new()
            {
                threadCount = _numThreadsRunning,
                duration = new TimeSpan(DateTime.Now.Ticks - _startTime.Ticks),
                filesToDelete = _previouslyRecordedFiles.Count,
                filesProcessed = _numFilesProcessed + _fileIndex,
                foldersProcessed = _numFoldersProcessed + _folderIndex,
                filesOutstanding = _files.Count - _fileIndex,
                foldersOutstanding = _folders.Count - _folderIndex,
                state = State,
            };
            if (res.filesOutstanding > 0)
                res.phase = IndexPhaseEnum.Scanning;
            else if (res.filesToDelete > 0)
            {
                res.phase = IndexPhaseEnum.Cleaning;
                res.filesOutstanding = res.filesToDelete;
            }
            else
                res.phase = IndexPhaseEnum.Done;
            return res;
        }

        public IndexManager()
        {
            _fileMutex = new object();
            _taskMutex = new object();
            _threadMutex = new object();
            State = StateEnum.Stopped;
        }

        //public void ExecuteAsync(string folder, int numThreads, string oneDriveUserName, string oneDrivePassword)
        public void ExecuteAsync(string folder, int numThreads)
        {
            Folder = folder;
            _startTime = DateTime.Now;

            if (String.IsNullOrEmpty(Folder)) throw new InvalidOperationException("Folder not specified");

            Abort();
            try
            {
                _folders = [];
                _folderIndex = 0; _fileIndex = 0; _numFilesProcessed = 0; _numFoldersProcessed = 0;
                _files = [];
                _threads = [];
                _numThreads = numThreads;
                _previouslyRecordedFiles = Config.GetDatabase().GetFilesByPathBrief(Folder);

                IndexWorker first = new(this);
                _threads.Add(first);
                first.ScanFolder(Folder);
                Config.LogDebugging("State: Running");
                State = StateEnum.Running;
                first.ExecuteAsync();
                for (int i = 1; i < _numThreads; i++)
                    AddWorker();
            }
            catch (Exception ex)
            {
                Config.WriteException(null, ex);
                throw;
            }
        }

        public void AddWorker()
        {
            if (State == StateEnum.Running)
            {
                IndexWorker w = new(this);
                _threads.Add(w);
                w.ExecuteAsync();
            }
        }

        public void Suspend()
        {
            if (State == StateEnum.Running)
            {
                Config.LogDebugging("State: Suspended");
                State = StateEnum.Suspended;
            }
        }

        public void Resume()
        {
            if (State == StateEnum.Suspended)
            {
                Config.LogDebugging("State: Running");
                State = StateEnum.Running;
            }
        }

        internal bool GetNextTask(out string folder, out string file)
        {
            const int CleanUp = 1000;
            folder = null;
            file = null;
            while (State == StateEnum.Suspended)
                System.Threading.Thread.Sleep(500);
            for (int i = 0; i < 2; i++)
            {
                lock (_taskMutex)
                {
                    if (_folders.Count > _folderIndex)
                    {
                        folder = _folders[_folderIndex++];
                        if (_folderIndex == CleanUp)
                        {
                            _folders.RemoveRange(0, CleanUp);
                            _folderIndex -= CleanUp;
                            _numFoldersProcessed += CleanUp;
                        }
                        return true;
                    }
                    if (_files.Count > _fileIndex)
                    {
                        file = _files[_fileIndex++];
                        if (_fileIndex == CleanUp)
                        {
                            _files.RemoveRange(0, CleanUp);
                            _fileIndex -= CleanUp;
                            _numFilesProcessed += CleanUp;
                        }
                        return true;
                    }
                }
                //If there is no work at present, wait to see if more turns up.
                System.Threading.Thread.Sleep(500);
            }
            return false;
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

        /// <summary>
        /// If we are concluding the final thread, then we now have a list of all hash references that no longer exist.
        /// Remove them from the database.
        /// </summary>
        private void Finalise()
        {
            if (State != StateEnum.Running || (_numThreadsRunning > 0))
                return;
            DereferenceMissingFiles();
            Config.LogDebugging("State: Stopped");
            State = StateEnum.Stopped;
        }

        private void DereferenceMissingFiles()
        {
            while (_previouslyRecordedFiles.Keys.Count > 0)
            {
                string f = _previouslyRecordedFiles.Values[0];
                try
                {
                    PathFormatted pf = new(f);
                    Config.LogInfo("Deleting record for " + pf.fullName + " as it is no longer found");
                    Config.GetDatabase().DeleteFile(pf);
                }
                catch (Exception ex)
                {
                    Config.WriteException(f, ex);
                }
                _previouslyRecordedFiles.RemoveAt(0);
            }
        }

        internal void AddFiles(string[] folders, string[] files)
        {
            lock (_taskMutex)
            {
                _files.AddRange(files);
                //Remove any files which have been found, even if we end up with an exception processing them.
                //Unicode filenames have issues here, for example: d:\misc\Tiffany\AMEB music\7th grade\Recordings\new\Telemann Fantasy No.6 in D Minor - Jasmine Choi 최나경.mp3
                //They aren't matched when extracting from the database.
                foreach (string f in files)
                    _previouslyRecordedFiles.Remove(f.ToUpper());
                _folders.AddRange(folders);
            }
        }

        public void Abort()
        {
            if (State == StateEnum.Stopped)
                return;
            Config.LogDebugging("State: Aborting");
            State = StateEnum.Aborting;
            for (int i = 0; i < _numThreads; i++)
                _threads[i].Abort();
            for (int i = 0; i < _numThreads; i++)
            {
                try
                {
                    _threads[i].Join();
                }
                catch { }
            }
            Config.LogDebugging("State: Stopped");
            State = StateEnum.Stopped;
        }

    }
}
