using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib2
{
    class HasherFarmer
    {
        private System.Threading.Thread _thread = null;
        private Hasher Parent;
        public bool IsComplete;
        private System.Security.Cryptography.SHA1 sha1;

        public HasherFarmer(Hasher parent)
        {
            sha1 = System.Security.Cryptography.SHA1.Create();
            Parent = parent;
            IsComplete = false;
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
                Parent.LogInfo("Starting");
                ThreadLoop(sleepMs, out folder, out file);
                Parent.LogInfo("Ending");
            }
            catch (System.Threading.ThreadAbortException) { }
            catch (Exception ex)
            {
                Parent.WriteException(null, ex);
                Parent.LogInfo(String.Format("Exception: {0}", ex.ToString()));
            }
            finally
            {
                Parent.LogInfo("Finalising");
                Parent.ThreadIsFinished();
            }
        }

        private void ThreadLoop(int sleepMs, out string folder, out string file)
        {
            Parent.LogInfo("Starting ThreadLoop");
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
                        Parent.LogInfo(String.Format("Pausing at attempt: {0}", attemptNo));
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
                Parent.LogInfo(String.Format("Task exception: {0}", ex.ToString()));
                if (attemptNo == maxAttempts)
                {
                    if (folder != null)
                        Parent.WriteException(folder, ex);
                    else
                        Parent.WriteException(file, ex);
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
            Parent.LogInfo(String.Format("Scanning: {0}", folder));
            string[] files = System.IO.Directory.GetFiles(folder);
            string[] folders = System.IO.Directory.GetDirectories(folder);
            Parent.AddFiles(folders, files);
        }

        private void HashFile(string file)
        {
            FileHash fh = new FileHash(file);
            if (!Parent.HasHash(fh))
            {
                Parent.LogInfo(String.Format("Hashing: {0}", file));
                fh.ComputeHash(sha1);
                Parent.StoreHash(fh);
            }
        }

        public void Abort(bool block)
        {
            try
            {
                _thread.Abort();
                if (block)
                    _thread.Join();
            }
            catch { }
        }
    }
}
