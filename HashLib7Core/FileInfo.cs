namespace HashLib7
{
    public class FileInfo
    {
        public string filePath;
        public long size;
        public string hash;
        public FileInfo() { }
        public FileInfo(string filePath)
        {
            this.filePath = filePath;
        }
    }
}
