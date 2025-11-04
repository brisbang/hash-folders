namespace HashLib7
{
    internal enum TaskStatusEnum
    {
        tseProcess,
        tseWait,
        tseFinished,
    }
    public class Task
    {
        internal FileInfo nextFile;
        internal string nextFolder;
        internal TaskStatusEnum status;
    }
}