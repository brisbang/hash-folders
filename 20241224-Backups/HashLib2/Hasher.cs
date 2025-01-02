using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashLib2
{
    public class Hasher
    {
        internal string Folder;
        private List<string> _folders;
        private int _folderIndex;
        private List<string> _files;
        private int _fileIndex;
        private List<HasherFarmer> _threads;
        private Library _library;
        private int _numThreadsRunning;
        private object _fileMutex;
        private object _taskMutex;
        private object _threadMutex;
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
            _library = Config.Library;
        }

        public void ExecuteAsync(string folder, int numThreads)
        {
            Folder = folder;

            if (String.IsNullOrEmpty(Folder)) throw new InvalidOperationException("Folder not specified");

            Abort();
            try
            {
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
                    Config.LogInfo(ex.Message);
                    throw;
                }
            }
            catch
            {
                throw;
            }
        }

        internal bool HasHash(FileHash fh)
        {
            FileHash s;
            if (!_library.TryGetValue(fh.Key, out s))
                return false;
            return FileHash.Compare(fh, s);
        }

        internal void StoreHash(FileHash fileHash)
        {
            _library.Add(fileHash);
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
