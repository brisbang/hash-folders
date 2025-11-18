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
            return Config.GetDatabase().GetFileInfoDetailed(filePath, Database.BackupLocationSearchEnum.AllowSameFolder);
        }

        public static FileComparisonList GetComparisonFolders(string path)
        {
            Database d = Config.GetDatabase();
            SortedList<string, FolderCount> locations = [];
            FileComparisonList res = new();
            string[] files = GetFiles(path);
            foreach (string file in files)
                res.Files.Add(AttachBackupsForFile(d, locations, file));
            res.FolderCounts = ConvertLocationsToFolderCount(path, locations);
            res.FolderCounts.Reverse();
            return res;
        }

        private static List<FolderCount> ConvertLocationsToFolderCount(string path, SortedList<string, FolderCount> locations)
        {
            List<FolderCount> res = [];
            foreach (FolderCount fc in locations.Values)
            {
                if (fc.Folder.ToUpper() != path.ToUpper())
                    res.Add(fc);
                else
                    break;
            }
            return res;
        }

        private static FileInfoDetailedComparison AttachBackupsForFile(Database d, SortedList<string, FolderCount> locations, string file)
        {
            const int maxBackups = 10;
            PathFormatted pf = new(file);
            FileInfoDetailedComparison fid = new(d.GetFileInfoDetailed(pf, Database.BackupLocationSearchEnum.ForceDifferentFolder));
            if (fid.FileInfo.BackupLocations.Count > maxBackups)
                fid.HasGeneralMatch = true;
            else
            {
                SortedList<string, bool> foundOptions = [];
                foreach (PathFormatted backupFolder in fid.FileInfo.BackupLocations)
                    AttachBackupFolder(locations, foundOptions, backupFolder.Path);
            }
            return fid;
        }

        private static void AttachBackupFolder(SortedList<string, FolderCount> locations, SortedList<string, bool> foundOptions, string backupPath)
        {
            if (!foundOptions.ContainsKey(backupPath))
            {
                foundOptions.Add(backupPath, false);
                if (locations.TryGetValue(backupPath, out FolderCount fc))
                    fc.Count++;
                else
                    locations.Add(backupPath, new() { Folder = backupPath, Count = 1 });
            }
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


