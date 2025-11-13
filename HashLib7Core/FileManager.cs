using System.Collections.Generic;
using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace HashLib7
{
    public class FileManager
    {
        public static void DeleteFile(PathFormatted filePath)
        {
            // Send to recycle bin
            FileSystem.DeleteFile(filePath.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

            // Notify database
            Config.GetDatabase().DeleteFile(filePath);

        }
        public static string[] GetFiles(string path)
        {
            return Directory.GetFiles(path);
        }

        public static FileInfoDetailed RetrieveFile(PathFormatted filePath)
        {
            return Config.GetDatabase().GetFileInfoDetailed(filePath, true);
        }

        public static FileComparisonList GetComparisonFolders(string path)
        {
            Database d = Config.GetDatabase();
            SortedList<string, FolderCount> locations = [];
            FileComparisonList res = new();
            string[] files = GetFiles(path);
            foreach (string file in files)
            {
                PathFormatted pf = new(file);
                FileInfoDetailed fid = d.GetFileInfoDetailed(pf, true);
                res.Files.Add(fid);
                FolderCount fc = null;
                SortedList<string, bool> foundOptions = [];
                foreach (PathFormatted backupFolder in fid.BackupLocations)
                {
                    string backupPath = backupFolder.Path;
                    if (!foundOptions.ContainsKey(backupPath))
                    {
                        foundOptions.Add(backupPath, false);
                        if (locations.TryGetValue(backupPath, out fc))
                            fc.Count++;
                        else
                            locations.Add(backupPath, new() { Folder = backupPath, Count = 1 });
                    }
                }
            }
            foreach (FolderCount fc in locations.Values)
            {
                if (fc.Folder.ToUpper() != path.ToUpper())
                    res.FolderCounts.Add(fc);
            }
            return res;
        }

        public static RiskAssessment GetRiskAssessment(PathFormatted filePath)
        {
            FileInfoDetailed info = RetrieveFile(filePath);
            RiskAssessment res = new(info);
            if (info.size == 0)
            {
                res.Theft = false;
                res.Corruption = false;
                res.DiskFailure = false;
                res.Fire = false;
            }
            else
            {
                var localDriveInfo = Config.Drives.Get(info.Path);
                List<HashLib7.DriveInfo> backupDriveInfos = [];
                if (info.BackupLocations != null)
                {
                    foreach (var backup in info.BackupLocations)
                    {
                        var driveInfo = Config.Drives.Get(backup.Path);
                        if (!backupDriveInfos.Contains(driveInfo))
                            backupDriveInfos.Add(driveInfo);
                    }
                }
                res.Theft = true;
                foreach (var driveInfo in backupDriveInfos)
                {
                    if (driveInfo.MitigatesRiskOfTheft(localDriveInfo))
                    {
                        res.Theft = false;
                        break;
                    }
                }
                res.Corruption = info.BackupLocations?.Count == 0;
                res.DiskFailure = true;
                if (localDriveInfo.MitigatesRiskOfDiskFailure(localDriveInfo))
                    res.DiskFailure = false;
                else
                {
                    foreach (var driveInfo in backupDriveInfos)
                    {
                        if (driveInfo.MitigatesRiskOfDiskFailure(localDriveInfo))
                        {
                            res.DiskFailure = false;
                            break;
                        }
                    }
                }
                res.Fire = true;
            }
            Config.GetDatabase().SaveRiskAssessment(res);
            return res;
        }
    }
}


