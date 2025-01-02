using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib2
{
    public class Config
    {
        private static Database _database;
        private static string _logfile;

        public static void SetParameters(string database, string logfile)
        {
            Database d = new Database();
            d.Open(database);
            if (_database != null)
                _database.Close();
            _database = d;
            int numFilesInLibrary = _database.NumFilesInLibrary();
        }

        internal static Database Database { get { return _database; } }
        internal static string Logfile {  get { return _logfile; } }
    }
}
