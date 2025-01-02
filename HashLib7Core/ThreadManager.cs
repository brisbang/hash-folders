using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace HashLib7
{
    public class ThreadManager
    {
        internal string Folder;
        private List<string> _folders;
        private int _folderIndex;
        private List<string> _files;
        private int _fileIndex;
        private List<Worker> _threads;
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
        private SortedList<string, short> _previouslyRecordedFiles;

        //GetStatistics can be out slightly due to work in progress.
        //If a thread is processing the last file then that information is not shown because the work queue is empty but NumThreadsRunning is not yet zero.
        public void GetStatistics(out int numFilesProcessed, out int numFoldersProcessed, out int numFilesOutstanding, out int numFoldersOutstanding, out int numThreadsRunning, out int numFilesToDelete)
        {
            lock (_taskMutex)
            {
                numFilesProcessed = _numFilesProcessed + _fileIndex;
                numFoldersProcessed = _numFoldersProcessed + _folderIndex;
                numFilesOutstanding = _files.Count - _fileIndex;
                numFoldersOutstanding = _folders.Count - _folderIndex;
                numFilesToDelete = _previouslyRecordedFiles.Count;
            }
            numThreadsRunning = _numThreadsRunning;
        }

        public ThreadManager()
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
            UserSettings.RecentlyUsedFolder = folder;
            UserSettings.ThreadCount = numThreads;

            if (String.IsNullOrEmpty(Folder)) throw new InvalidOperationException("Folder not specified");

            Abort();
            try
            {
                _folders = new List<string>();
                _folderIndex = 0; _fileIndex = 0; _numFilesProcessed = 0; _numFoldersProcessed = 0;
                _files = new List<string>();
                _threads = new List<Worker>();
                _numThreads = numThreads;
                _previouslyRecordedFiles = Config.Database.GetFilesByPathBrief(Folder);

                Worker first = new Worker(this);
                _threads.Add(first);
                first.ScanFolder(Folder);
                Config.LogInfo("State: Running");
                State = StateEnum.Running;
                first.ExecuteAsync();
                for (int i = 1; i < _numThreads; i++)
                    AddWorker();
            }
            catch (Exception ex)
            {
                Config.LogInfo(ex.Message);
                throw;
            }
        }

        public void AddWorker()
        {
            if (State == StateEnum.Running)
            {
                Worker w = new Worker(this);
                _threads.Add(w);
                w.ExecuteAsync();
            }
        }

        public void Suspend()
        {
            if (State == StateEnum.Running)
            {
                Config.LogInfo("State: Suspended");
                State = StateEnum.Suspended;
            }
        }

        public void Resume()
        {
            if (State == StateEnum.Suspended)
            {
                Config.LogInfo("State: Running");
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
            if (State == StateEnum.Running && (_numThreadsRunning == 0))
            {
                while (_previouslyRecordedFiles.Count > 0)
                { 
                    Config.Database.DeleteFile(new PathFormatted(_previouslyRecordedFiles.Keys[0]));
                    _previouslyRecordedFiles.RemoveAt(0);
                }
                Config.LogInfo("State: Stopped");
                State = StateEnum.Stopped;
            }
        }

        internal void AddFiles(string[] folders, string[] files)
        {
            lock (_taskMutex)
            {
                _files.AddRange(files);
                //Remove any files which have been found, even if we end up with an exception processing them.
                foreach (string f in files)
                    _previouslyRecordedFiles.Remove(f.ToUpper());
                _folders.AddRange(folders);
            }
        }

        public void Abort()
        {
            if (State == StateEnum.Stopped)
                return;
            Config.LogInfo("State: Aborting");
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
            Config.LogInfo("State: Stopped");
            State = StateEnum.Stopped;
        }

//        public Credentials Credentials { get { return _credentials; } }

    }
}
