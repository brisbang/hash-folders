using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class IndexDereferenceTask(AsyncManager parent, SortedList<string, string> previousFiles) : Task(parent, TaskStatusEnum.tseProcess)
    {
        private SortedList<string, string> PreviouslyRecordedFiles = previousFiles;

        public override string Verb => "Remove stale";

        public override string Target {
            get
            {
                string res = "";
                try
                {
                    res = PreviouslyRecordedFiles.Values[^1];
                }
                catch { }
                return res;
            }
        }

        public override void Execute()
        {
            Config.LogInfo("Removing " + PreviouslyRecordedFiles.Count + " stale entries");
            while (PreviouslyRecordedFiles.Keys.Count > 0)
            {
                string f = PreviouslyRecordedFiles.Values[^1];
                try
                {
                    PathFormatted pf = new(f);
                    Config.LogDebugging("Deleting record for " + pf.FullName + " as it is no longer found");
                    Config.GetDatabase().DeleteFile(pf);
                }
                catch (Exception ex)
                {
                    Config.WriteException(f, ex);
                }
                PreviouslyRecordedFiles.RemoveAt(PreviouslyRecordedFiles.Count - 1);
            }
         }
                

    }
}