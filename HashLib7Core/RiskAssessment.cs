namespace HashLib7
{
    public class RiskAssessment(FileInfoDetailed fileInfoDetailed)
    {
        public bool Theft;
        public bool Corruption;
        public bool DiskFailure;
        public bool Fire;
        public FileInfoDetailed FileInfoDetailed = fileInfoDetailed;
    }
}