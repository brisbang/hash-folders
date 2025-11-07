namespace HashLib7
{
    public class ReportHeaderTask(AsyncManager parent) : Task(parent, TaskStatusEnum.tseProcess)
    {
        public override void Execute()
        {
            ((ReportManager) Parent).LogDetail(ReportHeader());
        }

        public override void RegisterCompleted()
        {
            
        }

        public override string ToString()
        {
            return "Writing header";           
        }
        private static string ReportHeader()
        {
            string res = "Filename,Hash,Size,Local copies,Local backups,Remote backups,Local copy 1,Local backup 1,Remote backup 1\n";
            return res;
        }


    }
}