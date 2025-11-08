using System;
using System.Collections.Generic;

namespace HashLib7
{
    public class RATaskFile : TaskFile
    {
        public RATaskFile(AsyncManager parent, FileInfo file) : base(parent, file)
        {
        }

        public override string Verb => "Record RA";

        public override string Target => TargetFile.FullName;

        public override void Execute()
        {
            FileManager.GetRiskAssessment(new PathFormatted(base.TargetFile.FullName));
        }
   }
}