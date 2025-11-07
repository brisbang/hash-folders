using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class IndexGetPreviousFilesTask(AsyncManager parent, string folder) : TaskFolder(parent, folder)
    {
        public override string Verb => "Retrieve";

        public override string Target => "Previous files";

        public override void Execute()
        {
            ((IndexManager) Parent).previouslyRecordedFiles = Config.GetDatabase().GetFilesByPathBrief(this.nextFolder);
        }
    }
}