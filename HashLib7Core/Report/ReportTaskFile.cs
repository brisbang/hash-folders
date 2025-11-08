using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class ReportTaskFile(AsyncManager parent, FileInfo file) : TaskFile(parent, file)
    {
        public override string Verb => "Report";

        public override string Target => TargetFile.Path;

        public override void Execute()
        {
            ReportManager reportParent = (ReportManager)Parent;
            ReportRow rr = new()
            {
                hash = TargetFile.hash,
                filePath = TargetFile.FullName,
                size = TargetFile.size
            };
            FileLocations locations = new(TargetFile.FullName);
            if (TargetFile.size > 0)
            {
                List<PathFormatted> matchingFiles = Config.GetDatabase().GetFilesByHash(TargetFile.hash);
                foreach (PathFormatted match in matchingFiles)
                    locations.AddDuplicate(match);
            }
            RiskAssessment ra = FileManager.GetRiskAssessment(new PathFormatted(TargetFile.FullName));
            Config.LogDebugging("Logging file " + TargetFile.FullName);
            reportParent.LogDetail(ReportRowToString(locations, rr, ra));
        }

        private static string ReportRowToString(FileLocations locations, ReportRow rr, RiskAssessment ra)
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

            string res = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}\n",
                SafeFilename(rr.filePath),
                rr.hash,
                rr.size.ToString(),
                ra.DiskFailure ? "" : "Y",
                ra.Corruption ? "" : "Y",
                ra.Theft ? "" : "Y",
                ra.Fire ? "" : "Y",
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