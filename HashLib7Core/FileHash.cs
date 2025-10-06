using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace HashLib7
{
    internal class FileHash
    {
        private const string NoHash = "_NULL_";
        public string FilePath;

        private string _hash;
        public string Hash //Of the file itself
        {
            get { if (HasHash) return _hash; else return NoHash; }
        }
        public bool HasHash {  get { return (_hash != null) && (_hash != NoHash); } }
        public DateTime LastModified;
        public long Length;
        public string Key { get { return FilePath; } }

        private FileHash()
        { }

        public FileHash(string file)
        {
            FilePath = file;
            LastModified = Io.GetLastModified(file);
            LastModified = new DateTime(LastModified.Year, LastModified.Month, LastModified.Day, LastModified.Hour, LastModified.Minute, LastModified.Second); //Remove milliseconds for database comparison
            Length = Io.GetLength(file);
        }

        public FileHash(string file, DateTime lastModified, long length, string hash)
        {
            FilePath = file;
            LastModified = lastModified;
            Length = length;
            _hash = hash;
        }

        /// <summary>
        /// Computes the hash of file specified by the FilePath. May take some time.
        /// </summary>
        /// <param name="ha"></param>
        internal void Compute(HashAlgorithm ha)
        {
            _hash = ha.HashFile(FilePath);
        }

        public override string ToString()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\r\n", FilePath, Hash, Length.ToString(), LastModified.Ticks, LastModified.ToString());
        }


    }
}
