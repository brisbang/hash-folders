using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib3
{
    /// <summary>
    /// Pulls a task from the ThreadManager's backlog:
    /// * Scans a folder and expands the backlog of files (and further folders); or
    /// * Retrieves a file request, extracts information from the database, decides if it is out of date (or missing), and updates the data on record (if required).
    /// </summary>
    class Worker
    {
        private System.Threading.Thread _thread = null;
        private Hasher Parent;
        private HashAlgorithm _hashAlgorithm;

        public Worker(Hasher parent)
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
                string folder;
                string file;
                Parent.ThreadIsStarted();
                Config.LogInfo("Starting");
                ThreadLoop(sleepMs, out folder, out file);
                Config.LogInfo("Ending");
            }
            catch (System.Threading.ThreadAbortException) { }
            catch (Exception ex)
            {
                Config.WriteException(null, ex);
                Config.LogInfo(String.Format("Exception: {0}", ex.ToString()));
            }
            finally
            {
                Config.LogInfo("Finalising");
                Parent.ThreadIsFinished();
            }
        }

        private void ThreadLoop(int sleepMs, out string folder, out string file)
        {
            Config.LogInfo("Starting ThreadLoop");
            while (Parent.GetNextTask(out folder, out file))
            {
                int attemptNo = 0;
                bool success = false;
                while (!success)
                {
                    success = ExecuteTask(sleepMs, folder, file, attemptNo);
                    if (!success)
                    {
                        attemptNo++;
                        Config.LogInfo(String.Format("Pausing at attempt: {0}", attemptNo));
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
                Config.LogInfo(String.Format("Task exception: {0}", ex.ToString()));
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
                Config.LogInfo(String.Format("Scanning: {0}", folder));
            string[] files = System.IO.Directory.GetFiles(folder);
            string[] folders = System.IO.Directory.GetDirectories(folder);
            Parent.AddFiles(folders, files);
        }

        private void HashFile(string file)
        {
            FileHash fh = new FileHash(file);
            if (RequiresUpdatedHash(fh))
            {
                if (Config.LogDebug)
                    Config.LogInfo(String.Format("Hashing: {0}", file));
                fh.Compute(_hashAlgorithm);
                Config.Database.WriteHash(fh);
            }
        }

        /// <summary>
        /// An updated hash is required if:
        /// * There is no record in the database
        /// * The filesize has changed
        /// * The modified time has changed
        /// </summary>
        /// <param name="fh"></param>
        /// <returns></returns>
        private bool RequiresUpdatedHash(FileHash fh)
        {
            FileHash recorded = Config.Database.ReadHash(fh.FilePath);
            if (recorded == null)
                return true;
            if (fh.LastModified != recorded.LastModified)
                return true;
            if (fh.Length != recorded.Length)
                return true;
            return false;
        }

        public void Abort(bool block)
        {
            try
            {
                if (_thread.ThreadState != System.Threading.ThreadState.Stopped)
                {
                    _thread.Abort();
                    if (block)
                        _thread.Join();
                }
            }
            catch { }
        }
    }
}
