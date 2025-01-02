using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib3
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
        public string Key { get { return FilePath.ToUpper(); } }

        private FileHash()
        { }

        public FileHash(string file)
        {
            FilePath = file;
            LastModified = System.IO.File.GetLastWriteTime(file);
            Length = new System.IO.FileInfo(file).Length;
        }

        public FileHash(System.Data.OleDb.OleDbDataReader reader)
        {
            FilePath = reader[0] as string;
            _hash = reader[1] as string;
            Length = long.Parse(reader[2] as string);
            LastModified = new DateTime(long.Parse(reader[3] as string));
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
