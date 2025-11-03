using System;
using System.Collections.Generic;

namespace HashLib7
{
    class ReportWorker : IWorker
    {
        private System.Threading.Thread _thread = null;
        private readonly ReportManager Parent;

        public ReportWorker(ReportManager parent)
        {
            Parent = parent;
            _thread = new System.Threading.Thread(ExecuteInternal);
        }
        
        public void ExecuteAsync()
        {
            _thread.Start();
        }

        private void ExecuteInternal()
        {
            Parent.ThreadIsStarted();
            try
            {
                Database d = Config.GetDatabase();
                PathFormatted p = new(Parent.Path);
                Parent.TryWriteHeader();
                bool finished = false;
                while (!finished && ShouldProcessNextTask())
                {
                    ReportTaskEnum task = Parent.GetNextTask(out FileInfo file);
                    switch (task)
                    {
                        case ReportTaskEnum.rteGetFiles:
                            Parent.SetFiles(d.GetFilesByPath(Parent.Path));
                            break;
                        case ReportTaskEnum.rteWaitingForFiles:
                            System.Threading.Thread.Sleep(500);
                            break;
                        case ReportTaskEnum.rteProcessFile:
                            ReportFile(d, file);
                            break;
                        case ReportTaskEnum.rteNoMoreFiles:
                            finished = true;
                            break;
                        default: throw new InvalidOperationException("Unknown ReportTaskEnum " + task.ToString());
                    }

                }
            }
            catch
            { }
            finally
            {
                Parent.ThreadIsFinished();
            }
        }

        private bool ShouldProcessNextTask()
        {
            while (true)
            {
                switch (Parent.State)
                {
                    case StateEnum.Stopped: return false; //Weird
                    case StateEnum.Aborting: return false;
                    case StateEnum.Suspended:
                        System.Threading.Thread.Sleep(500);
                        break;
                    case StateEnum.Running: return true;
                    default: throw new Exception("Unknown Parent.State" + Parent.State.ToString());
                }
            }
        }

        private void ReportFile(Database d, FileInfo file)
        {
            ReportRow rr = new()
            {
                hash = file.hash,
                filePath = file.filePath,
                size = file.size
            };
            FileLocations locations = new(file.filePath);
            if (file.size > 0)
            {
                List<PathFormatted> matchingFiles = d.GetFilesByHash(file.hash);
                foreach (PathFormatted match in matchingFiles)
                    locations.AddDuplicate(match);
            }
            Config.LogDebugging("Logging file " + file.filePath);
            Parent.LogLine(ReportRowToString(locations, rr), true);
        }

        private static string ReportRowToString(FileLocations locations, ReportRow rr)
        {
            List<string> localCopies = locations.Copies(LocationEnum.LocalCopy);
            List<string> localBackups = locations.Copies(LocationEnum.LocalBackup);
            List<string> remoteBackups = locations.Copies(LocationEnum.RemoteBackup);
            string localCopyFirst = String.Empty;
            string localBackupFirst = String.Empty;
            string remoteBackupFirst = String.Empty;
            if (localCopies.Count > 0) localCopyFirst = SafeFilename(localCopies[0]);
            if (localBackups.Count > 0) localBackupFirst = SafeFilename(localBackups[0]);
            if (remoteBackups.Count > 0) remoteBackupFirst = SafeFilename(remoteBackups[0]);

            string res = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}\n", SafeFilename(rr.filePath), rr.hash, rr.size.ToString(), 
               localCopies.Count.ToString(),
               localBackups.Count.ToString(),
               remoteBackups.Count.ToString(),
               localCopyFirst,
               localBackupFirst,
               remoteBackupFirst);
            return res;
        }

        private static string SafeFilename(string filename)
        {
            return filename.Replace(',', '|');
        }
    }
}