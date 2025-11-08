using System;

namespace HashLib7
{
    public class ReportHeaderTask(AsyncManager parent) : Task(parent, TaskStatusEnum.tseProcess)
    {
        public override string Verb => "Initialise header";

        public override string Target => "";

        public override void Execute()
        {
            ReportManager rmParent = (ReportManager)Parent;
            rmParent.OutputFile = String.Format("{0}\\Report-{1}.csv", Config.DataPath, rmParent.StartTime.ToString("yyyy-MM-dd-HHmmss"));
            rmParent.LogDetail(ReportHeader());
            rmParent.FoldersToProcess.Add(rmParent.Path);
            rmParent.InitialTaskInProgress = false;
        }

        private static string ReportHeader()
        {
            string res = "Filename,Hash,Size,Disk Failure,Corruption,Theft,Fire,Local copies,Local backups,Remote backups,Local copy 1,Local backup 1,Remote backup 1\n";
            return res;
        }


    }
}