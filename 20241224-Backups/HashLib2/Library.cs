using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib2
{
    internal class Library
    {
        private SortedList<string, FileHash> _library;
        private static object _mutex;
        private static Database _database;
        internal static Database Database { get { return _database; } }

        internal Library(string database)
        {
            Database d = new Database();
            d.Open(database);
            if (_database != null)
                _database.Close();
            _database = d;
            _mutex = new object();
        }

        internal void ReadAll()
        {
            Config.LogInfo("Reading library");
            _library = new SortedList<string, FileHash>();
            System.Data.OleDb.OleDbDataReader reader = Database.GetLibrary();
            try
            {
                while (reader.Read())
                {
                    FileHash fh = new FileHash(reader);
                    _library.Add(fh.Key, fh);
                }
            }
            finally
            {
                reader.Close();
            }

        }

        public bool TryGetValue(string key, out FileHash hash)
        {
            lock (_mutex)
            {
                return _library.TryGetValue(key, out hash);
            }
        }

        public int Count { get { return _library.Count; } }

        public void Add(FileHash hash)
        {
            lock (_mutex)
            {
                _library.Add(hash.Key, hash);
            }
            Database.WriteHash(hash.FilePath, hash.HashFilePath, hash.Hash, hash.Length, hash.LastModified);
        }
    }
}
