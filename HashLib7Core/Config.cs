using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib7
{
    public class Config
    {
        private static string _dataPath;
        private static string _logFile;
        private static string _databaseFile;
        private static object _logMutex;
        private static System.IO.FileStream _outputLog;
        public static bool LogDebug { get; private set; }
        internal static Database Database { get; private set; }

        public static void SetParameters(string dataPath, string database, string logFile, bool debug)
        {
            _dataPath = dataPath;
            _logFile = logFile;
            _databaseFile = database;
            _logMutex = new object();
            _outputLog = System.IO.File.OpenWrite(Config.LogFile);
            LogDebug = debug;
            Database = new Database(Config.DatabaseFile);
        }

        internal static string LogFile { get { return String.Format("{0}\\{1}", DataPath, _logFile); } }
        internal static string DataPath { get { return _dataPath; } }
        internal static string DatabaseFile { get { return String.Format("{0}\\{1}", DataPath, _databaseFile); } }
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
