using System;
using System.Collections.Generic;
using System.Threading;

namespace HashLib7
{

    public abstract class AsyncManager
    {

        public StateEnum State { get; protected set; } = StateEnum.Stopped;
        public string Path { get; private set; }
        private readonly List<Worker> Workers = [];
        public int NumThreadsDesired { get; private set; }
        public int NumThreadsToFinish = 0;
        public int NumThreadsRunning { get { if (Workers == null) return 0; return Workers.Count; }}
        public int FoldersBeingProcessed { get; private set; }
        public int FilesBeingProcessed { get; private set; }
        protected internal List<string> FoldersToProcess = [];
        protected internal List<FileInfo> FilesToProcess = [];
        private List<string> FilesCompleted = [];
        private List<string> FoldersCompleted = [];
        private bool HasInitialised = false;
        private bool AllowCreateThreads = true;
        private bool DeliveredInitialTask = false;
        private bool DeliveredFinalTask = false;
        protected object MutexFilesFolders = new();
        private readonly object MutexWorkers = new();
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public TimeSpan Duration
        {
            get
            {
                if (EndTime == DateTime.MaxValue)
                    return new(DateTime.Now.Ticks - StartTime.Ticks);
                return new(EndTime.Ticks - StartTime.Ticks);
            }
        }
        public int NumFilesProcessed
        {
            get
            {
                return FilesCompleted.Count;
            }
        }

        public int NumFoldersScanned
        {
            get
            {
                return FoldersCompleted.Count;
            }
        }
        public int NumFilesOutstanding
        {
            get
            {
                return FilesToProcess.Count + FilesBeingProcessed;
            }
        }
        public int NumFoldersOutstanding
        {
            get
            {
                return FoldersToProcess.Count + FoldersBeingProcessed;
            }
        }

        internal AsyncManager()
        { }

        protected virtual Task GetInitialTask() { return null; }
        protected virtual Task GetFinalTask() { return null; }
        protected internal virtual void AddFilesInvoked(List<FileInfo> files) { }

        public abstract ManagerStatus GetStatus();
        public abstract Task GetFolderTask(string folder);
        public abstract Task GetFileTask(FileInfo file);

        protected List<WorkerStatus> GetWorkerStatuses()
        {
            List<WorkerStatus> res = [];
            lock (MutexWorkers)
            {
                foreach (Worker w in Workers)
                    res.Add(w.Status);
            }
            return res;
        }

        public void ExecuteAsync(string path, int numThreads)
        {
            if (String.IsNullOrEmpty(path)) throw new InvalidOperationException("Folder not specified");
            if (numThreads <= 0) throw new InvalidOperationException("Number of threads must be positive");
            if (NumThreadsRunning > 0) throw new InvalidOperationException("Cannot ExecuteAsync whilst threads exist");
            Path = path;
            FoldersToProcess = []; FoldersToProcess.Add(path);
            FilesToProcess = [];
            FoldersCompleted = [];
            FilesCompleted = [];
            NumThreadsDesired = numThreads;
            StartTime = DateTime.Now;
            IncreaseThreadsToDesired();
        }

        private void IncreaseThreadsToDesired()
        {
            lock (MutexWorkers)
            {
                if (AllowCreateThreads)
                {
                    while (Workers.Count < NumThreadsDesired)
                    {
                        Worker w = new(this);
                        Workers.Add(w);
                        w.ExecuteAsync();
                    }
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
                        if (NumThreadsToFinish > 0) //Do we need to cut back threads? Let's do it.
                        {
                            NumThreadsToFinish--;
                            return null;
                        }
                        if (!DeliveredInitialTask)
                        {
                            DeliveredInitialTask = true;
                            Task initial = GetInitialTask();
                            if (initial != null)
                                return initial;
                        }
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
                                if ((FoldersBeingProcessed > 0) || (FilesBeingProcessed > 0))
                                {
                                    //We wait until everything is done before heading to Finalise
                                    Config.LogDebugging("Waiting...");
                                    return new TaskWait(this);
                                }
                            }
                            AllowCreateThreads = false;
                            if (NumThreadsRunning == 1) //Last thread - let's see if we should deliver the final task
                            {
                                if (!DeliveredFinalTask)
                                {
                                    DeliveredFinalTask = true;
                                    Task final = GetFinalTask();
                                    if (final != null)
                                        return final;
                                }
                            }
                            EndTime = DateTime.Now;
                            return null;
                        }
                    default: throw new Exception("Unknown StateEnum!");
                }
            }
        }
        
        public void Abort()
        {
            if (this.State == StateEnum.Running || this.State == StateEnum.Paused)
                this.State = StateEnum.Stopping;
        }

        public void Pause()
        {
            if (this.State == StateEnum.Running)
                this.State = StateEnum.Paused;
        }

        public void Play()
        {
            if (this.State == StateEnum.Paused)
                this.State = StateEnum.Running;
        }

        internal void ThreadIsStarted()
        {
            lock (MutexWorkers)
            {
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
            lock (MutexWorkers)
            {
                if (NumThreadsToFinish > 0)
                {
                    NumThreadsToFinish--;
                    return;
                }
            }
            IncreaseThreadsToDesired();
        }
        
        public void ThreadDec()
        {
            if (NumThreadsDesired <= 1)
                return;
            NumThreadsDesired--;
            lock (MutexWorkers)
            {
                NumThreadsToFinish++;
            }
            //Somehow signal to the last worker that it is not going to continue.
            //Is it in GetNextTask, that it should pass in its id?
            //Should finished tasks be removed from the worker pool?
        }

        internal void ThreadIsFinished(Worker w)
        {
            if (NumThreadsRunning == 1) //So this is the last thread finishing up
                Finalise();
            lock (MutexWorkers)
            {
                Workers.Remove(w);
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
                {
                    FilesToProcess.AddRange(files);
                    AddFilesInvoked(files);
                }
            }
        }

        internal void FolderScanned(string folderScanned)
        {
            lock (MutexFilesFolders)
            {
                FoldersCompleted.Add(folderScanned);
                FoldersBeingProcessed--;
            }
        }

        internal void FileScanned(string file)
        {
            lock (MutexFilesFolders)
            {
                FilesCompleted.Add(file);
                FilesBeingProcessed--;
            }
        }
   }
}