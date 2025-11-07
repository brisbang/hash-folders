namespace HashLib7
{
    public abstract class TaskFolder(AsyncManager parent, string folder) : Task(parent, TaskStatusEnum.tseProcess)
    {
        internal string nextFolder = folder;

        public override void RegisterCompleted()
        {
            try
            {
                Parent.FolderScanned(nextFolder);
            }
            catch { }
        }

        public override string ToString()
        {
            return "Scanning: " + nextFolder;
        }
    }
}