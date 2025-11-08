namespace HashLib7
{
    public abstract class TaskFolder(AsyncManager parent, string folder) : Task(parent, TaskStatusEnum.tseProcess)
    {
        internal string TargetFolder = folder;

        public override void RegisterCompleted()
        {
            try
            {
                Parent.FolderScanned(TargetFolder);
            }
            catch { }
        }

    }
}