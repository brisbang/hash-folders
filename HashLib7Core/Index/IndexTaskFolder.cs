using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class IndexTaskFolder : TaskFolder
    {
        public IndexTaskFolder(AsyncManager parent, string folder) : base(parent, folder)
        {

        }
        
        public override void Execute()
        {
            if (Config.LogDebug)
                Config.LogDebugging(String.Format("Scanning: {0}", this.nextFolder));
            string[] fileList = Io.GetFiles(this.nextFolder);
            List<FileInfo> files = [];
            //Could be inefficient
            foreach (string file in fileList)
                files.Add(new FileInfo(file));
            List<string> folders = [];
            folders.AddRange(Io.GetFolders(this.nextFolder));
            Parent.AddFoldersAndFiles(folders, files);
        }
    }
}