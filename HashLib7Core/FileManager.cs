using System.ComponentModel;
using Microsoft.VisualBasic.FileIO;

namespace HashLib7
{
    public class FileManager
    {
        public static void DeleteFile(string filePath)
        {
            // Send to recycle bin
            FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

            // Notify database
            Config.GetDatabase().DeleteFile(new PathFormatted(filePath));

        }

        public static FileInfoDetailed RetrieveFile(string filePath)
        {
            PathFormatted pf = new(filePath);
            return Config.GetDatabase().GetFileInfoDetailed(pf, true);
        }
    }
}