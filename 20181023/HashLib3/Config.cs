using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib3
{
    public class Config
    {
        private static string _logfile;
        private static object _logMutex;
        private static System.IO.FileStream _outputLog;
        internal static Database Database { get; private set; }

        public static void SetParameters(string database, string logfile)
        {
            _logfile = logfile;
            _logMutex = new object();
            _outputLog = System.IO.File.OpenWrite(String.Format("{0}", Config.Logfile));
            Database = new Database();
            Database.Open(database);
            LogInfo(String.Format("Opened connection to {0}", database));
        }

        internal static string Logfile {  get { return _logfile; } }
        internal static void Close()
        {
            try
            {
                _outputLog.Close();
            }
            finally { }
        }

        public static void LogInfo(string text)
        {
            string output = String.Format("[{0}]:{1}\r\n", System.Threading.Thread.CurrentThread.ManagedThreadId, text);
            byte[] outputBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(output);
            lock (_logMutex)
            {
                _outputLog.Write(outputBytes, 0, outputBytes.Length);
                _outputLog.Flush();
            }
        }

        internal static void WriteException(string file, Exception ex)
        {
            if (file == null)
                file = "<No information>";
            string output = String.Format("{0}\t{1}\r\n", file, ex.ToString());
            byte[] outputBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(output);
            lock (_logMutex)
            {
                _outputLog.Write(outputBytes, 0, outputBytes.Length);
                _outputLog.Flush();
            }

        }

    }
}
