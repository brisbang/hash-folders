using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HashLib7
{
    public class Config
    {
        private static string _dataPath;
        private static string _databaseFile;
        private static object _logMutex;
        private static ILogger<Config> _logger;
        public static bool LogDebug { get; private set; }
        internal static Database Database { get; private set; }
        private static IServiceProvider _provider;

        public static void SetParameters(IServiceProvider provider, string dataPath, string database, bool logDebug)
        {
            _provider = provider;
            _dataPath = dataPath;
            _databaseFile = database;
            _logMutex = new object();
            _logger = provider.GetRequiredService<ILogger<Config>>();
            LogDebug = logDebug;
        }

        internal static Database GetDatabase()
        {
            return new Database(_provider.GetRequiredService<ILogger<Database>>(), DatabaseFile);
        }

        internal static string DataPath { get { return _dataPath; } }
        internal static string DatabaseFile { get { return String.Format("{0}\\{1}", DataPath, _databaseFile); } }

        public static void LogInfo(string text)
        {
            string output = String.Format("[{0}]:{1}\r\n", System.Threading.Thread.CurrentThread.ManagedThreadId, text);
            _logger.LogInformation(output);
        }

        internal static void WriteException(string file, Exception ex)
        {
            if (file == null)
                file = "<No information>";
            string output = String.Format("{0}\t{1}\r\n", file, ex.ToString());
            _logger.LogError(output);
        }

    }
}
