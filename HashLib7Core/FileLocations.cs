using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph.Models;

namespace HashLib7
{
    internal class FileLocations
    {
        private string _sourceFilename;
        private char _sourceDrive;
        //Sure - we could make it more abstract but optimisation matters too.
        private List<string> _localCopies;
        private List<string> _localBackups;
        private List<string> _remoteBackups;

        public List<string> Copies(LocationEnum location)
        {
            return location switch
            {
                LocationEnum.LocalBackup => _localBackups,
                LocationEnum.LocalCopy => _localCopies,
                LocationEnum.RemoteBackup => _remoteBackups,
                LocationEnum.SameFile => null,
                _ => throw new InvalidEnumArgumentException("Unknonwn location: " + location.ToString()),
            };
        }

        public FileLocations(string sourceFilename) {
            _sourceFilename = sourceFilename;
            _sourceDrive = Char.ToUpper(sourceFilename[0]);
            _localBackups = [];
            _localCopies = [];
            _remoteBackups = [];
        }

        public string SourceFile {
            get {return _sourceFilename;}
        }

        public void AddDuplicate(string filename)
        {
            List<string> list = ListForTarget(filename);
            list?.Add(filename);
        }

        private List<string> ListForTarget(string targetFilename)
        {
            return Copies(Compare(_sourceFilename, _sourceDrive, targetFilename));
        }

        private static LocationEnum Compare(string sourceFilename, char sourceDrive, string targetFilename)
        {
            const char limitOfLocalDrives = 'M';
            char targetDrive = Char.ToUpper(targetFilename[0]);
            if (sourceFilename == targetFilename)
                return LocationEnum.SameFile;
            if (sourceFilename[0] == targetFilename[0])
                return LocationEnum.LocalCopy;
            if (sourceDrive < limitOfLocalDrives && targetDrive < limitOfLocalDrives)
                return LocationEnum.LocalBackup;
            return LocationEnum.RemoteBackup;
        }
    }
}

