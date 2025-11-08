using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class IndexTaskFolder(AsyncManager parent, string folder) : TaskFolder(parent, folder)
    {
        public override string Verb => "Scan";

        public override string Target => base.TargetFolder;

        public override void Execute()
        {
            if (Config.LogDebug)
                Config.LogDebugging(String.Format("Scanning: {0}", this.TargetFolder));
            string[] fileList = Io.GetFiles(this.TargetFolder);
            List<FileInfo> files = [];
            //Could be inefficient
            foreach (string file in fileList)
                files.Add(new FileInfo(file));
            List<string> folders = [];
            folders.AddRange(Io.GetFolders(this.TargetFolder));
            Parent.AddFoldersAndFiles(folders, files);
        }
    }
}