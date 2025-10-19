using System.ComponentModel;
using Microsoft.VisualBasic.FileIO;

namespace HashLib7
{
    public class FileManager
    {
        public static void DeleteFile(PathFormatted filePath)
        {
            // Send to recycle bin
            FileSystem.DeleteFile(filePath.fullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

            // Notify database
            Config.GetDatabase().DeleteFile(filePath);

        }

        public static FileInfoDetailed RetrieveFile(PathFormatted filePath)
        {
            return Config.GetDatabase().GetFileInfoDetailed(filePath, true);
        }
    }
}