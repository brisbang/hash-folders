using System;
using System.Collections.Generic;

namespace HashLib7
{
    class ReportWorker(AsyncManager parent) : Worker(parent)
    {
        protected override void Execute()
        {
            const int pauseMs = 500;
            try
            {
                ReportManager reportParent = (ReportManager)Parent;
                Database d = Config.GetDatabase();
                PathFormatted p = new(reportParent.Path);
                bool finished = false;
                while (!finished && ShouldProcessNextTask())
                {
                    Task task = reportParent.GetNextTask();
                    switch (task.status)
                    {
                        case TaskStatusEnum.tseProcess:
                            if (task.nextFile != null)
                            {
                                ReportFile(d, task.nextFile);
                                reportParent.FileScanned(task.nextFile.filePath);
                            }
                            else
                                reportParent.FolderScanned(task.nextFolder, null, d.GetFilesByPath(task.nextFolder));
                            break;
                        case TaskStatusEnum.tseWait:
                            System.Threading.Thread.Sleep(pauseMs);
                            break;
                        case TaskStatusEnum.tseFinished:
                            finished = true;
                            break;
                        default: throw new InvalidOperationException("Unknown ReportTaskEnum " + task.ToString());
                    }

                }
            }
            catch
            { }
        }

        private void ReportFile(Database d, FileInfo file)
        {
            ReportManager reportParent = (ReportManager)Parent;
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
            reportParent.LogDetail(ReportRowToString(locations, rr));
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