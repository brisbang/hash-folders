namespace HashLib7
{
    public class ReportTaskFolder(AsyncManager parent, string folder) : TaskFolder(parent, folder)
    {
        public override void Execute()
        {
            Parent.AddFoldersAndFiles(null, Config.GetDatabase().GetFilesByPath(nextFolder));
        }
    }
}