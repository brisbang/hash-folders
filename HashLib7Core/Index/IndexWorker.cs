using System;
using System.Collections.Generic;

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
    internal class IndexWorker(AsyncManager parent) : Worker(parent)
    {
        private HashAlgorithm _hashAlgorithm = new();

        protected override void Execute(Task task)
        {
            if (task.nextFile != null)
                HashFile(task.nextFile.filePath);
            else
                ScanFolder(task.nextFolder);
        }

        public void ScanFolder(string folder)
        {
            if (Config.LogDebug)
                Config.LogDebugging(String.Format("Scanning: {0}", folder));
            string[] fileList = Io.GetFiles(folder);
            List<FileInfo> files = [];
            //Could be inefficient
            foreach (string file in fileList)
                files.Add(new FileInfo(file));
            List<string> folders = [];
            folders.AddRange(Io.GetFolders(folder));
            Parent.AddFoldersAndFiles(folders, files);
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
    }
}
