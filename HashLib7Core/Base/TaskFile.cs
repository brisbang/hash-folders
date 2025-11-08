namespace HashLib7
{
    public abstract class TaskFile(AsyncManager parent, FileInfo file) : Task(parent, TaskStatusEnum.tseProcess)
    {
        internal FileInfo nextFile = file;

        public override void RegisterCompleted()
        {
            try
            {
                Parent.FileScanned(nextFile.Path);
            }
            catch { }
        }
    }
}