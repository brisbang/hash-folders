using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class IndexGetPreviousFilesTask : TaskFolder
    {
        public IndexGetPreviousFilesTask(AsyncManager parent, string folder) : base(parent, folder)
        {

        }
        
        public override void Execute()
        {
            ((IndexManager) Parent).previouslyRecordedFiles = Config.GetDatabase().GetFilesByPathBrief(this.nextFolder);
        }
        
        public override string ToString()
        {
            return "Retrieve previously recorded files";
        }
    }
}