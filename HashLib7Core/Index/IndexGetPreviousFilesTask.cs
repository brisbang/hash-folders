using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HashLib7
{
    public class IndexGetPreviousFilesTask(AsyncManager parent, string folder) : Task(parent, TaskStatusEnum.tseProcess)
    {
        public override string Verb => "Previous file search";

        public override string Target => Folder;

        private readonly string Folder = folder;

        public override void Execute()
        {
            IndexManager parent = (IndexManager)Parent;
            parent.previouslyRecordedFiles = Config.GetDatabase().GetFilesByPathBrief(Folder);
            parent.FoldersToProcess.Add(parent.Path);
            parent.InitialTaskInProgress = false;
        }
    }
}