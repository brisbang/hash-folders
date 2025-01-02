using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib2
{
    public class FileHash
    {
        private const string NoHash = "_NULL_";
        public string FilePath;
        private string _hash;
        public string Hash
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

        public void WriteToFile(System.IO.FileStream output)
        {
            byte[] bytes = System.Text.ASCIIEncoding.ASCII.GetBytes(ToString());
            output.Write(bytes, 0, bytes.Length);
            output.Flush();
        }

        public static bool Compare(FileHash f1, FileHash f2)
        {
            if ((f1.Key == f2.Key) && (f1.LastModified == f2.LastModified) && (f1.Length == f2.Length))
                return true;
            if ((f1.Hash == f2.Hash) && (f1.HasHash))
                return true;
            return false;
        }

        public override string ToString()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\r\n", FilePath, Hash, Length.ToString(), LastModified.Ticks, LastModified.ToString());
        }

        public static FileHash ReadFileHash(long lineNumber, string line)
        {
            const int expectedNumItems = 5;
            FileHash res = new FileHash();
            string[] items = line.Split('\t');
            if (items.Length != expectedNumItems)
                throw new Exception(String.Format("Expected {0} items in line. Actual {1}. Line is: {2}", expectedNumItems, items.Length, line));
            try
            {
                res.FilePath = items[0];
                res._hash = items[1];
                res.Length = long.Parse(items[2]);
                res.LastModified = new DateTime(long.Parse(items[3]));
                //Only use the tick count for last modified. Final field is purely for visual information.
            }
            catch
            {
                throw new Exception(String.Format("Could not read line {0} into FileHash: {1}", lineNumber, line));
            }
            return res;
        }

        public void ComputeHash(System.Security.Cryptography.HashAlgorithm hasher)
        {
            System.IO.FileStream inputStream = System.IO.File.OpenRead(FilePath);
            byte[] hash = hasher.ComputeHash(inputStream);
            _hash = ByteArrayToString(hash);
        }

        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

    }
}
