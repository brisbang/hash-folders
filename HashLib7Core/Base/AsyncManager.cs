using System;
using System.Collections.Generic;

namespace HashLib7
{

    public abstract class AsyncManager
    {

        public StateEnum State { get; protected set; } = StateEnum.Stopped;
        public string Path { get; private set; }
        private List<Worker> _threads;
        public int NumThreads { get; private set; }
        public int NumThreadsRunning { get; private set; }
        public int FoldersBeingProcessed { get; private set; }
        public int FilesBeingProcessed { get; private set; }
        protected internal List<string> FoldersToProcess;
        protected internal List<FileInfo> FilesToProcess;
        private List<string> _filesCompleted;
        private List<string> _foldersCompleted;
        protected object MutexFilesFolders = new();
        private object _mutexThread = new();
        private object _mutexExecute = new();
        protected abstract List<Worker> ExecuteInvoked(int numThreads);
        public DateTime StartTime { get; private set; }
        public TimeSpan Duration => new(DateTime.Now.Ticks - StartTime.Ticks);
        public int NumFilesProcessed => _filesCompleted.Count;
        public int NumFoldersScanned => _foldersCompleted.Count;
        public int NumFilesOutstanding => FilesToProcess.Count + FilesBeingProcessed;
        public int NumFoldersOutstanding => FoldersToProcess.Count + FoldersBeingProcessed;

        internal AsyncManager()
        { }

        protected virtual void InitialiseInvoked() { }
        protected virtual void FinaliseInvoked() { }
        protected internal virtual void AddFilesInvoked(List<FileInfo> files) { }

        public void ExecuteAsync(string path, int numThreads)
        {
            if (String.IsNullOrEmpty(path)) throw new InvalidOperationException("Folder not specified");
            if (numThreads <= 0) throw new InvalidOperationException("Number of threads must be positive");
            Path = path;
            FoldersToProcess = []; FoldersToProcess.Add(path);
            FilesToProcess = [];
            _foldersCompleted = [];
            _filesCompleted = [];
            NumThreads = numThreads;
            StartTime = DateTime.Now;
            lock (_mutexExecute)
            {
                if (_threads != null)
                    throw new InvalidOperationException("Cannot ExecuteAsync whilst threads exist");
                _threads = ExecuteInvoked(NumThreads);
                foreach (Worker w in _threads)
                    w.ExecuteAsync();
            }
        }

        public DateTime TimeRemaining()
        {
            long nfp = NumFilesProcessed;
            long nfc = NumFilesOutstanding + nfp;
            if (NumFilesProcessed > 0)
            {
                if (this.State == StateEnum.Running || this.State == StateEnum.Suspended)
                {
                    return DateTime.MinValue.AddTicks((long)((double)nfc * (DateTime.Now.Ticks - StartTime.Ticks) / nfp));
                }
            }
            return DateTime.MinValue;
        }

        internal Task GetNextTask()
        {
            Task res = new();
            lock (MutexFilesFolders)
            {
                if (FoldersToProcess.Count > 0)
                {
                    res.status = TaskStatusEnum.tseProcess;
                    res.nextFolder = GetFolderToProcess();
                    Config.LogDebugging("Processing folder");
                }
                else
                {
                    if (FilesToProcess.Count > 0)
                    {
                        res.status = TaskStatusEnum.tseProcess;
                        res.nextFile = GetFileToProcess();
                        Config.LogDebugging("Processing file");
                    }
                    else
                    {
                        if (FoldersBeingProcessed > 0)
                        {
                            //We wait until everything is done before heading to Finalise
                            res.status = TaskStatusEnum.tseWait;
                            Config.LogDebugging("Waiting...");
                        }
                        else //If only files are left, then you can stop here.
                        {
                            res.status = TaskStatusEnum.tseFinished;
                            Config.LogDebugging("Finished");
                        }
                    }
                }
            }
            return res;
        }
        
        public void Abort()
        {
            if (_threads == null)
                throw new InvalidOperationException("ExecuteAsync not invoked");
            if (this.State == StateEnum.Running || this.State == StateEnum.Suspended)
                this.State = StateEnum.Aborting;
        }

        public void Suspend()
        {
            if (_threads == null)
                throw new InvalidOperationException("ExecuteAsync not invoked");
            if (this.State == StateEnum.Running)
                this.State = StateEnum.Suspended;
        }

        public void Resume()
        {
            if (_threads == null)
                throw new InvalidOperationException("ExecuteAsync not invoked");
            if (this.State == StateEnum.Suspended)
                this.State = StateEnum.Running;
        }

        internal void ThreadIsStarted()
        {
            lock (_mutexThread)
            {
                ++NumThreadsRunning;
                if (NumThreadsRunning == 1)
                    Initialise();
            }
        }

        internal void ThreadIsFinished()
        {
            lock (_mutexThread)
            {
                if (NumThreadsRunning == 1)
                    Finalise();
                --NumThreadsRunning;
            }
        }

        private void Initialise()
        {
            Config.LogDebugging("State: Starting");
            State = StateEnum.Running;
            InitialiseInvoked();
        }

        private void Finalise()
        {
            FinaliseInvoked();
            Config.LogDebugging("State: Stopped");
            State = StateEnum.Stopped;
        }

        protected string GetFolderToProcess()
        {
            string res = FoldersToProcess[^1];
            FoldersToProcess.RemoveAt(FoldersToProcess.Count - 1);
            FoldersBeingProcessed++;
            return res;
        }

        protected internal FileInfo GetFileToProcess()
        {
            FileInfo res = FilesToProcess[^1];
            FilesToProcess.RemoveAt(FilesToProcess.Count - 1);
            FilesBeingProcessed++;
            return res;
        }

        internal void AddFoldersAndFiles(List<string> folders, List<FileInfo> files)
        {
            lock (MutexFilesFolders)
            {
                if (folders != null)
                    FoldersToProcess.AddRange(folders);
                if (files != null)
                    FilesToProcess.AddRange(files);
                AddFilesInvoked(files);
            }
        }

        internal void FolderScanned(string folderScanned)
        {
            lock (MutexFilesFolders)
            {
                _foldersCompleted.Add(folderScanned);
                FoldersBeingProcessed--;
            }
        }

        internal void FileScanned(string file)
        {
            lock (MutexFilesFolders)
            {
                _filesCompleted.Add(file);
                FilesBeingProcessed--;
            }
        }
   }
}