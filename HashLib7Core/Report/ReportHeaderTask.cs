namespace HashLib7
{
    public class ReportHeaderTask(AsyncManager parent) : Task(parent, TaskStatusEnum.tseProcess)
    {
        public override string Verb => "Initialise header";

        public override string Target => "";

        public override void Execute()
        {
            ReportManager rmParent = (ReportManager)Parent;
            rmParent.LogDetail(ReportHeader());
            rmParent.HasCompletedHeader = true;
        }

        private static string ReportHeader()
        {
            string res = "Filename,Hash,Size,Local copies,Local backups,Remote backups,Local copy 1,Local backup 1,Remote backup 1\n";
            return res;
        }


    }
}