using System;
using System.Collections.Generic;
using System.Threading;

namespace HashLib7
{
    public class IndexManager : AsyncManager
    {
        //All previously known files under the folder. After processing is complete, any files in this list were not found and so can be removed from the database.
        //The short is not needed and so left 0 for performance reasons
        private SortedList<string, string> previouslyRecordedFiles = [];
        private readonly object mutexRecordedFiles = new();

        //GetStatistics can be out slightly due to work in progress.
        //If a thread is processing the last file then that information is not shown because the work queue is empty but NumThreadsRunning is not yet zero.
        public override TaskStatus GetStatus()
        {
            IndexStatus res = new()
            {
                threadCount = base.NumThreadsRunning,
                duration = base.Duration,
                filesToDelete = previouslyRecordedFiles.Count,
                filesProcessed = NumFilesProcessed,
                foldersProcessed = NumFoldersScanned,
                filesOutstanding = NumFilesOutstanding,
                foldersOutstanding = NumFoldersOutstanding,
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

        protected override List<Worker> ExecuteInvoked(int numThreads)
        {

            List<Worker> threads = [];
            for (int i = 0; i < numThreads; i++)
                threads.Add(new Worker(this));
            return threads;
        }

        protected override void InitialiseInvoked()
        {
            previouslyRecordedFiles = Config.GetDatabase().GetFilesByPathBrief(base.FoldersToProcess[0]);
        }

        /// <summary>
        /// If we are concluding the final thread, then we now have a list of all hash references that no longer exist.
        /// Remove them from the database.
        /// </summary>
        protected override void FinaliseInvoked()
        {
            DereferenceMissingFiles();
        }

        private void DereferenceMissingFiles()
        {
            //Shouldn't be necessary given external events
            lock (mutexRecordedFiles)
            {
                Config.LogInfo("Removing " + previouslyRecordedFiles.Count + " stale entries");
                while (previouslyRecordedFiles.Keys.Count > 0)
                {
                    string f = previouslyRecordedFiles.Values[^1];
                    try
                    {
                        PathFormatted pf = new(f);
                        Config.LogDebugging("Deleting record for " + pf.fullName + " as it is no longer found");
                        Config.GetDatabase().DeleteFile(pf);
                    }
                    catch (Exception ex)
                    {
                        Config.WriteException(f, ex);
                    }
                    previouslyRecordedFiles.RemoveAt(previouslyRecordedFiles.Count - 1);
                }
            }
        }

        protected internal override void AddFilesInvoked(List<FileInfo> files)
        {
            lock (mutexRecordedFiles)
            {
                //Remove any files which have been found, even if we end up with an exception processing them.
                //Unicode filenames have issues here, for example: d:\misc\Tiffany\AMEB music\7th grade\Recordings\new\Telemann Fantasy No.6 in D Minor - Jasmine Choi 최나경.mp3
                //They aren't matched when extracting from the database.
                foreach (FileInfo f in files)
                    previouslyRecordedFiles.Remove(f.filePath.ToUpper());
            }
        }

        public override Task GetFolderTask(string folder)
        {
            return new IndexTaskFolder(this, folder);
        }

        public override Task GetFileTask(FileInfo file)
        {
            return new IndexTaskFile(this, file);
        }
    }
}
