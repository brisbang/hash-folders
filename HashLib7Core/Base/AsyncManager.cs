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
        private bool HasInitialised = false;
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

        protected virtual Task GetInitialTask() { return null;  }
        protected virtual Task GetFinalTask() { return null;  }
        protected internal virtual void AddFilesInvoked(List<FileInfo> files) { }

        public abstract ManagerStatus GetStatus();
        public abstract Task GetFolderTask(string folder);
        public abstract Task GetFileTask(FileInfo file);

        protected List<WorkerStatus> GetWorkerStatuses()
        {
            List<WorkerStatus> res = [];
            lock (_mutexThread)
            {
                foreach (Worker w in _threads)
                {
                    res.Add(w.Status);
                }
            }
            return res;
        }

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
                if (this.State == StateEnum.Running || this.State == StateEnum.Paused)
                {
                    return DateTime.MinValue.AddTicks((long)((double)nfc * (DateTime.Now.Ticks - StartTime.Ticks) / nfp));
                }
            }
            return DateTime.MinValue;
        }

        internal Task GetNextTask()
        {
            lock (MutexFilesFolders)
            {
                switch (this.State)
                {
                    case StateEnum.Paused:
                        return new TaskWait(this);
                    case StateEnum.Stopping:
                    case StateEnum.Stopped:
                    case StateEnum.Undefined:
                        return null;
                    case StateEnum.Running:
                        if (FoldersToProcess.Count > 0)
                        {
                            Config.LogDebugging("Processing folder");
                            return GetFolderToProcess();
                        }
                        else
                        {
                            if (FilesToProcess.Count > 0)
                            {
                                Config.LogDebugging("Processing file");
                                return GetFileToProcess();
                            }
                            else
                            {
                                if (FoldersBeingProcessed > 0)
                                {
                                    //We wait until everything is done before heading to Finalise
                                    Config.LogDebugging("Waiting...");
                                    return new TaskWait(this);
                                }
                                else //If only files are left, then you can stop here.
                                    return null;
                            }
                        }
                    default: throw new Exception("Unknown StateEnum!");
                }
            }
        }
        
        public void Abort()
        {
            if (_threads == null)
                throw new InvalidOperationException("ExecuteAsync not invoked");
            if (this.State == StateEnum.Running || this.State == StateEnum.Paused)
                this.State = StateEnum.Stopping;
        }

        public void Suspend()
        {
            if (_threads == null)
                throw new InvalidOperationException("ExecuteAsync not invoked");
            if (this.State == StateEnum.Running)
                this.State = StateEnum.Paused;
        }

        public void Resume()
        {
            if (_threads == null)
                throw new InvalidOperationException("ExecuteAsync not invoked");
            if (this.State == StateEnum.Paused)
                this.State = StateEnum.Running;
        }

        internal void ThreadIsStarted()
        {
            lock (_mutexThread)
            {
                ++NumThreadsRunning;
                if (!HasInitialised)
                {
                    HasInitialised = true;
                    Initialise();
                }
            }
        }

        internal void ThreadIsFinished()
        {
            lock (_mutexThread)
            {
                if (NumThreadsRunning == 1) //So this is the last thread finishing up
                    Finalise();
                --NumThreadsRunning;
            }
        }

        private void Initialise()
        {
            Config.LogDebugging("State: Starting");
            State = StateEnum.Running;
            GetInitialTask()?.Execute();
        }

        private void Finalise()
        {
            if (State != StateEnum.Stopping)
                GetFinalTask()?.Execute();
            Config.LogDebugging("State: Stopped");
            State = StateEnum.Stopped;
        }

        protected Task GetFolderToProcess()
        {
            string res = FoldersToProcess[^1];
            FoldersToProcess.RemoveAt(FoldersToProcess.Count - 1);
            FoldersBeingProcessed++;
            return GetFolderTask(res);
        }

        protected internal Task GetFileToProcess()
        {
            FileInfo res = FilesToProcess[^1];
            FilesToProcess.RemoveAt(FilesToProcess.Count - 1);
            FilesBeingProcessed++;
            return GetFileTask(res);
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