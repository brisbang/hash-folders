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

        public static FileInfoDetailed RetrieveFile(PathFormatted filePath)
        {
            return Config.GetDatabase().GetFileInfoDetailed(filePath, true);
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
                if (info.backupLocations != null)
                {
                    foreach (var backup in info.backupLocations)
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
                res.Corruption = info.backupLocations?.Count == 0;
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