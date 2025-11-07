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

    public class IndexTaskFile : TaskFile
    {
        public IndexTaskFile(AsyncManager parent, FileInfo file) : base(parent, file)
        {

        }
        
        public override void Execute()
        {
            FileHash fh = new(this.nextFile.filePath);
            RecordMatch match = RequiresUpdatedHash(fh);
            if (match == RecordMatch.Match)
                return;
            Config.LogDebugging(String.Format("Hashing: {0}", this.nextFile.filePath));
            fh.Compute();
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
        public override string ToString()
        {
            return "Indexeing: " + base.nextFile;
        }
    }
}