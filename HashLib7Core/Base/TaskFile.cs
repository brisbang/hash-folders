namespace HashLib7
{
    public abstract class TaskFile(AsyncManager parent, FileInfo file) : Task(parent, TaskStatusEnum.tseProcess)
    {
        internal FileInfo TargetFile = file;

        public override void RegisterCompleted()
        {
            try
            {
                Parent.FileScanned(TargetFile.Path);
            }
            catch { }
        }
    }
}