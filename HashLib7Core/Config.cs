using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HashLib7
{
    public class Config
    {
        private static string _dataPath;
        private static string _connStr;
        public static DriveInfoList Drives { get; private set; }
        private static ILogger<Config> _logger;
        public static bool LogDebug { get; private set; }
        private static IServiceProvider _provider;

        public static void SetParameters(IServiceProvider provider, string dataPath, string connStr, DriveInfoList drives, bool logDebug)
        {
            _provider = provider;
            _dataPath = dataPath;
            _connStr = connStr;
            _logger = provider.GetRequiredService<ILogger<Config>>();
            Drives = drives;
            LogDebug = logDebug;
        }

        internal static Database GetDatabase()
        {
            return new Database(_provider.GetRequiredService<ILogger<Database>>(), _connStr);
        }

        internal static string DataPath { get { return _dataPath; } }

        public static void LogInfo(string text)
        {
            string output = String.Format("[{0}]:{1}\r\n", System.Threading.Thread.CurrentThread.ManagedThreadId, text);
            Console.WriteLine(output);
            _logger.LogInformation(output);
        }

        public static void LogDebugging(string text)
        {
            if (LogDebug)
            {
                string output = String.Format("[{0} / [{1}]:{2}\r\n", System.DateTime.Now.ToString(), System.Threading.Thread.CurrentThread.ManagedThreadId, text);
                Console.WriteLine(output);
                _logger.LogDebug(output);
            }
        }

        internal static void WriteException(string file, Exception ex)
        {
            file ??= "<No information>";
            string output = String.Format("{0}\t{1}\r\n", file, ex.ToString());
            Console.Error.WriteLine(output);
            _logger.LogError(output);
        }

    }
}
