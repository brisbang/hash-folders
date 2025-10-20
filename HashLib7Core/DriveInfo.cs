using System.Collections;
using System.Collections.Generic;
using Microsoft.Graph.Models;

namespace HashLib7
{
    public class DriveInfo(char letter, int numDrives, string location)
    {
        public char Letter { get; set; } = letter;
        public string Location { get; set; } = location;
        public int NumDrives { get; set; } = numDrives;
        public bool MitigatesRiskOfDiskFailure(DriveInfo source) => (source.Letter != Letter) || source.NumDrives > 1 || NumDrives > 1;

        public bool MitigatesRiskOfTheft(DriveInfo source) => source.Location != Location;
    }

    public class DriveInfoList
    {
        private SortedList<char, DriveInfo> _drives = [];

        public void Add(DriveInfo drive)
        {
            _drives.Add(drive.Letter, drive);
        }

        public DriveInfo Get(string path)
        {
            return _drives[path.ToUpper()[0]];
        }

        public List<DriveInfo> GetAll()
        {
            return new List<DriveInfo>(_drives.Values);
        }   
    }
}