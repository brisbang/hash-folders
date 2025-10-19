using System;

namespace HashLib7
{
    internal enum RecordMatch
    {
        /// <summary>
        /// A record was found but the details do not match
        /// </summary>
        NoMatch,
        /// <summary>
        /// The details match the record on file
        /// </summary>
        Match,
        /// <summary>
        /// No record was found
        /// </summary>
        NoRecord
    }

    /// <summary>
    /// Pulls a task from the ThreadManager's backlog:
    /// * Scans a folder and expands the backlog of files (and further folders); or
    /// * Retrieves a file request, extracts information from the database, decides if it is out of date (or missing), and updates the data on record (if required).
    /// </summary>
    class Worker
    {
        private System.Threading.Thread _thread = null;
        private readonly ThreadManager Parent;
        private HashAlgorithm _hashAlgorithm;
        private bool _aborted;

        public Worker(ThreadManager parent)
        {
            _hashAlgorithm = new HashAlgorithm();
            Parent = parent;
            _thread = new System.Threading.Thread(Execute);
        }

        public void ExecuteAsync()
        {
            _thread.Start();
        }

        private void Execute()
        {
            try
            {
                const int sleepMs = 100;
                Parent.ThreadIsStarted();
                _aborted = false;
                Config.LogDebugging("Worker starting");
                ThreadLoop(sleepMs, out string folder, out string file);
                Config.LogDebugging("Worker ending");
            }
            catch (System.Threading.ThreadAbortException) { }
            catch (Exception ex)
            {
                Config.WriteException(null, ex);
            }
            finally
            {
                Config.LogDebugging("Finalising");
                Parent.ThreadIsFinished();
            }
        }

        private void ThreadLoop(int sleepMs, out string folder, out string file)
        {
            Config.LogDebugging("Starting ThreadLoop");
            while (Parent.GetNextTask(out folder, out file) && !_aborted)
            {
                int attemptNo = 0;
                bool success = false;
                while (!success && !_aborted)
                {
                    success = ExecuteTask(sleepMs, folder, file, attemptNo);
                    if (!success && !_aborted)
                    {
                        attemptNo++;
                        Config.LogDebugging(String.Format("Pausing at attempt: {0}", attemptNo));
                        //In a fatal error - let's try again after a brief pause.
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
        }

        private bool ExecuteTask(int sleepMs, string folder, string file, int attemptNo)
        {
            const int maxAttempts = 10;
            try
            {
                ExecuteTaskInternal(sleepMs, folder, file);
                return true;
            }
            catch (System.Threading.ThreadAbortException) { throw; }
            catch (Exception ex)
            {
                if (attemptNo == maxAttempts)
                {
                    if (folder != null)
                        Config.WriteException(folder, ex);
                    else
                        Config.WriteException(file, ex);
                    //Give up
                    return true;
                }
                else
                    //Signal a retry
                    return false;
            }
        }

        private void ExecuteTaskInternal(int sleepMs, string folder, string file)
        {
            if (folder != null)
                ScanFolder(folder);
            else // (file != null)
                HashFile(file);
        }

        public void ScanFolder(string folder)
        {
            if (Config.LogDebug)
                Config.LogDebugging(String.Format("Scanning: {0}", folder));
            string[] files = Io.GetFiles(folder);
            string[] folders = Io.GetFolders(folder);
            Parent.AddFiles(folders, files);
        }

        private void HashFile(string file)
        {
            FileHash fh = new(file);
            RecordMatch match = RequiresUpdatedHash(fh);
            if (match == RecordMatch.Match)
                return;
            Config.LogDebugging(String.Format("Hashing: {0}", file));
            fh.Compute(_hashAlgorithm);
            Config.GetDatabase().WriteHash(fh, match == RecordMatch.NoRecord);
        }

        /// <summary>
        /// An updated hash is required if:
        /// * There is no record in the database
        /// * The filesize has changed
        /// * The modified time has changed
        /// </summary>
        /// <param name="fh"></param>
        /// <returns></returns>
        private static RecordMatch RequiresUpdatedHash(FileHash fh)
        {
            FileHash recorded = Config.GetDatabase().ReadHash(fh.FilePath);
            if (recorded == null)
                return RecordMatch.NoRecord;
            if (fh.LastModified != recorded.LastModified)
                return RecordMatch.NoMatch;
            if (fh.Length != recorded.Length)
                return RecordMatch.NoMatch;
            return RecordMatch.Match;
        }

        public void Abort()
        {
            //Nowadays you have to farm the task off to a process to abort it safely. I'm not re-engineering for that.
            _aborted = true;
        }

        public void Join()
        {
            try
            {
                if (_thread.ThreadState != System.Threading.ThreadState.Stopped)
                {
                    _thread.Join();
                }
            }
            catch { }
        }
    }
}
