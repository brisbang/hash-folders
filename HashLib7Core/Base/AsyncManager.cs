using System;
using System.Collections.Generic;
using System.Threading;

namespace HashLib7
{

    public abstract class AsyncManager
    {

        public StateEnum State { get; protected set; } = StateEnum.Stopped;
        public string Path { get; private set; }
        private List<Worker> Workers;
        public int NumThreadsDesired { get; private set; }
        public int NumThreads { get { if (Workers == null) return 0; return Workers.Count; }}
        public int NumThreadsRunning { get; private set; }
        public int FoldersBeingProcessed { get; private set; }
        public int FilesBeingProcessed { get; private set; }
        protected internal List<string> FoldersToProcess;
        protected internal List<FileInfo> FilesToProcess;
        private List<string> _filesCompleted;
        private List<string> _foldersCompleted;
        private bool HasInitialised = false;
        protected object MutexFilesFolders = new();
        private object _mutexWorkers = new();
        private object _mutexExecute = new();
        private int NumThreadsPendingIdle = 0;
        public DateTime StartTime { get; private set; }
        public TimeSpan Duration => new(DateTime.Now.Ticks - StartTime.Ticks);
        public int NumFilesProcessed
        {
            get
            {
                if (_filesCompleted == null)
                    return 0;
                return _filesCompleted.Count;
            }
        }

        public int NumFoldersScanned
        {
            get
            {
                if (_foldersCompleted == null)
                    return 0;
                return _foldersCompleted.Count;
            }
        }
        public int NumFilesOutstanding
        {
            get
            {
                if (FilesToProcess == null)
                    return 0;
                return FilesToProcess.Count + FilesBeingProcessed;
            }
        }
        public int NumFoldersOutstanding
        {
            get
            {
                if (FoldersToProcess == null)
                    return 0;
                return FoldersToProcess.Count + FoldersBeingProcessed;
            }
        }

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
            if (Workers != null)
            {
                lock (_mutexWorkers)
                {
                    if (Workers != null)
                    {
                        foreach (Worker w in Workers)
                        {
                            res.Add(w.Status);
                        }
                    }
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
            NumThreadsDesired = numThreads;
            StartTime = DateTime.Now;
            lock (_mutexWorkers)
            {
                if (Workers != null)
                    throw new InvalidOperationException("Cannot ExecuteAsync whilst threads exist");
                Workers = [];
            }
            IncreaseThreadsToDesired();
        }

        private void IncreaseThreadsToDesired()
        {
            lock (_mutexWorkers)
            {
                while (Workers.Count < NumThreadsDesired)
                {
                    Worker w = new(this);
                    Workers.Add(w);
                    w.ExecuteAsync();
                }
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
            if (Workers == null)
                throw new InvalidOperationException("ExecuteAsync not invoked");
            if (this.State == StateEnum.Running || this.State == StateEnum.Paused)
                this.State = StateEnum.Stopping;
        }

        public void Pause()
        {
            if (Workers == null)
                throw new InvalidOperationException("ExecuteAsync not invoked");
            if (this.State == StateEnum.Running)
                this.State = StateEnum.Paused;
        }

        public void Play()
        {
            if (Workers == null)
                throw new InvalidOperationException("ExecuteAsync not invoked");
            if (this.State == StateEnum.Paused)
                this.State = StateEnum.Running;
        }

        internal void ThreadIsStarted()
        {
            lock (_mutexWorkers)
            {
                ++NumThreadsRunning;
                if (!HasInitialised)
                {
                    HasInitialised = true;
                    Initialise();
                }
            }
        }

        public void ThreadInc()
        {
            NumThreadsDesired++;
        }
        
        public void ThreadDec()
        {
            if (NumThreadsDesired <= 1)
                return;
            NumThreadsDesired--;
            //Somehow signal to the last worker that it is not going to continue.
            //Is it in GetNextTask, that it should pass in its id?
            //Should finished tasks be removed from the worker pool?
        }

        internal void ThreadIsFinished()
        {
            lock (_mutexWorkers)
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