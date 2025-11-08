namespace HashLib7
{
    public class RATaskFolder(AsyncManager parent, string folder) : TaskFolder(parent, folder)
    {
        public override string Verb => "Scan";

        public override string Target => TargetFolder;

        public override void Execute()
        {
            Parent.AddFoldersAndFiles(null, Config.GetDatabase().GetFilesByPath(TargetFolder));
        }
    }
}