using System.Collections.ObjectModel;
using System.IO;

namespace HashFolders
{
    public class FolderItem
    {
        public string Path { get; set; }
        public string Name => System.IO.Path.GetFileName(Path) ?? Path;
        public ObservableCollection<FolderItem> SubFolders { get; set; } = new();

        private bool _isLoaded;

        public FolderItem()
        {
            SubFolders.Add(null); // Dummy to show expand arrow
        }

        public void LoadSubFolders()
        {
            if (_isLoaded) return;
            SubFolders.Clear();

            try
            {
                foreach (var dir in Directory.GetDirectories(Path))
                {
                    SubFolders.Add(new FolderItem { Path = dir });
                }
            }
            catch { /* Ignore access errors */ }

            _isLoaded = true;
        }
    }
}
