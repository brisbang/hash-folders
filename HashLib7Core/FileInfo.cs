namespace HashLib7
{
    public class FileInfo : PathFormatted
    {
        public long size;
        public string hash;

        public FileInfo(string filePath) : base(filePath)
        {
        }

        public FileInfo(string path, string name) : base(path, name)
        { }
    }
}
