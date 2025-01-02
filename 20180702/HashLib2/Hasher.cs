using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib2
{
    public class Hasher
    {
        internal string Destination;
        internal string Folder;
        private List<string> _folders;
        private int _folderIndex;
        private List<string> _files;
        private int _fileIndex;
        private List<HasherFarmer> _threads;
        private SortedList<string, FileHash> _library;
        private int _numThreadsRunning;
        private object _fileMutex;
        private object _taskMutex;
        private object _threadMutex;
        private object _logMutex;
        private object _libraryMutex;
        private System.IO.FileStream _output;
        private System.IO.FileStream _outputLog;
        private int _numThreads;
        private int _numFilesProcessed;
        private int _numFoldersProcessed;

        //GetStatistics can be out slightly due to work in progress.
        //If a thread is processing the last file then that information is not shown because the work queue is empty but NumThreadsRunning is not yet zero.
        public bool GetStatistics(out int numFilesProcessed, out int numFoldersProcessed, out int numFilesOutstanding, out int numFoldersOutstanding, out int numThreadsRunning, out int libraryCount)
        {
            if (_library == null) throw new InvalidOperationException("Cannot get statistics when library is empty");
            lock (_taskMutex)
            {
                numFilesProcessed = _numFilesProcessed + _fileIndex;
                numFoldersProcessed = _numFoldersProcessed + _folderIndex;
                numFilesOutstanding = _files.Count - _fileIndex;
                numFoldersOutstanding = _folders.Count - _folderIndex;
                libraryCount = _library.Count;
            }
            lock (_threadMutex)
            {
                numThreadsRunning = _numThreadsRunning;
                return _numThreadsRunning == 0;
            }
        }

        public Hasher()
        {
            _fileMutex = new object();
            _taskMutex = new object();
            _threadMutex = new object();
            _logMutex = new object();
            _libraryMutex = new object();
        }

        public void ExecuteAsync(string destination, string folder, int numThreads)
        {
            Destination = destination;
            Folder = folder;

            if (String.IsNullOrEmpty(Destination)) throw new InvalidOperationException("Destination not specified");
            if (String.IsNullOrEmpty(Folder)) throw new InvalidOperationException("Folder not specified");

            Abort();
            _outputLog = System.IO.File.OpenWrite(String.Format("{0}.log", Destination));
            try
            {
                ReadLibrary();
                //This replaces the library to erase duplicates
                if (_library.Count > 0)
                    WriteLibrary();
                _output = System.IO.File.Open(Destination, System.IO.FileMode.Append);
                try
                {
                    _folders = new List<string>();
                    _folderIndex = 0; _fileIndex = 0; _numFilesProcessed = 0; _numFoldersProcessed = 0;
                    _files = new List<string>();
                    _threads = new List<HasherFarmer>();
                    _numThreads = numThreads;
                    for (int i = 0; i < _numThreads; i++)
                    {
                        _threads.Add(new HasherFarmer(this));
                        if (i == 0)
                            //Initialise the work queue
                            _threads[0].ScanFolder(Folder);
                        _threads[i].ExecuteAsync();
                    }
                }
                catch (Exception ex)
                {
                    LogInfo(ex.Message);
                    _output.Close();
                    throw;
                }
            }
            catch
            {
                _outputLog.Close();
                throw;
            }
        }

        private void WriteLibrary()
        {
            try
            {
                string backup = String.Format("{0}.{1}", Destination, DateTime.Now.ToString("yyyyMMdd-Hhmmss"));
                if (System.IO.File.Exists(Destination))
                    System.IO.File.Move(Destination, backup);
                _output = System.IO.File.Open(Destination, System.IO.FileMode.Create);
                try
                {
                    lock (_fileMutex)
                    {
                        foreach (FileHash fh in _library.Values)
                            StoreHashOnFile(fh);
                    }
                }
                finally
                {
                    _output.Close();
                }

            }
            catch (Exception ex)
            {
                LogInfo(String.Format("Unable to write library file. Serious but not critical. Exception: {0}", ex.ToString()));
            }
        }

        private void ReadLibrary()
        {
            const int maxExceptionCount = 100;
            LogInfo("Reading library");
            _library = new SortedList<string, FileHash>();
            System.IO.StreamReader input;
            if (!System.IO.File.Exists(Destination))
            {
                LogInfo("No library found. Starting fresh");
                return;
            }
            input = System.IO.File.OpenText(Destination);
            int lineNumber = 1;
            int exceptionCount = 0;
            try
            {
                while (!input.EndOfStream)
                {
                    string line = input.ReadLine();
                    try
                    {
                        lineNumber++;
                        FileHash fh = FileHash.ReadFileHash(lineNumber, line);
                        _library.Add(fh.Key, fh);
                    }
                    catch (Exception ex)
                    {
                        //This occurs when a file update occurred. It's not an issue.
                        if (ex.Message != "An entry with the same key already exists.")
                        {
                            LogInfo(String.Format("Unable to read line {0}. Exception: {1}", lineNumber, ex.ToString()));
                            if (++exceptionCount == maxExceptionCount)
                                throw new Exception(String.Format("Aborting library read. {0} exceptions reached.", maxExceptionCount));
                        }
                    }
                }
            }
            finally
            {
                input.Close();
            }

        }

        internal bool HasHash(FileHash fh)
        {
            FileHash s;
            lock (_libraryMutex)
            {
                if (!_library.TryGetValue(fh.Key, out s))
                    return false;
            }
            return FileHash.Compare(fh, s);
        }

        public void LogInfo(string text)
        {
            string output = String.Format("[{0}]:{1}\r\n", System.Threading.Thread.CurrentThread.ManagedThreadId, text);
            byte[] outputBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(output);
            lock (_logMutex)
            {
                _outputLog.Write(outputBytes, 0, outputBytes.Length);
                _outputLog.Flush();
            }
        }

        internal void StoreHash(FileHash fileHash)
        {
            StoreHashInMemory(fileHash);
            StoreHashOnFile(fileHash);
        }

        private void StoreHashInMemory(FileHash fileHash)
        {
            lock (_libraryMutex)
            {
                _library.Add(fileHash.Key, fileHash);
            }
        }

        private void StoreHashOnFile(FileHash fileHash)
        {
            lock (_fileMutex)
            {
                fileHash.WriteToFile(_output);
            }
        }

        internal bool GetNextTask(out string folder, out string file)
        {
            const int CleanUp = 1000;
            folder = null;
            file = null;
            for (int i = 0; i < 2; i++)
            {
                lock (_taskMutex)
                {
                    if (_folders.Count > _folderIndex)
                    {
                        folder = _folders[_folderIndex++];
                        if (_folderIndex == CleanUp)
                        {
                            _folders.RemoveRange(0, CleanUp);
                            _folderIndex -= CleanUp;
                            _numFoldersProcessed += CleanUp;
                        }
                        return true;
                    }
                    if (_files.Count > _fileIndex)
                    {
                        file = _files[_fileIndex++];
                        if (_fileIndex == CleanUp)
                        {
                            _files.RemoveRange(0, CleanUp);
                            _fileIndex -= CleanUp;
                            _numFilesProcessed += CleanUp;
                        }
                        return true;
                    }
                }
                //If there is no work at present, wait to see if more turns up.
                System.Threading.Thread.Sleep(500);
            }
            return false;
        }

        internal void ThreadIsStarted()
        {
            lock (_threadMutex)
            {
                ++_numThreadsRunning;
            }
        }

        internal void ThreadIsFinished()
        {
            lock (_threadMutex)
            {
                --_numThreadsRunning;
                if (_numThreadsRunning == 0)
                { 
                    _output.Close();
                    //Take a final backup.
                    WriteLibrary();
                    _outputLog.Close();
                }
            }

        }

        internal void AddFiles(string[] folders, string[] files)
        {
            lock (_taskMutex)
            {
                _files.AddRange(files);
                _folders.AddRange(folders);
            }
        }

        internal void WriteException(string file, Exception ex)
        {
            if (file == null)
                file = "<No information>";
            string output = String.Format("{0}\t{1}\r\n", file, ex.ToString());
            byte[] outputBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(output);
            lock (_fileMutex)
            {
                _outputLog.Write(outputBytes, 0, outputBytes.Length);
                _outputLog.Flush();
            }

        }

        public void Abort()
        {
            for (int i = 0; i < _numThreads; i++)
            {
                try
                {
                    if (!_threads[i].IsComplete)
                        _threads[i].Abort(true);
                }
                catch { }
            }
        }
    }
}
