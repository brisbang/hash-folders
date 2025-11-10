using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace HashFolders
{
    public partial class FolderComparison : Window
    {
        private string initialFolder;
        private List<string> folderList;

        public FolderComparison(string folder, List<string> folders)
        {
            InitializeComponent();
            initialFolder = folder;
            folderList = folders;

            LeftFolder.Content = folder;

            FolderSelector.ItemsSource = folderList;
            FolderSelector.SelectedIndex = 0;

            LoadLeftFiles();
            LoadRightFiles(folderList[0]);
        }

        private void LoadLeftFiles()
        {
            if (Directory.Exists(initialFolder))
            {
                LeftFileList.ItemsSource = Directory.GetFiles(initialFolder).Select(Path.GetFileName).ToList();
            }
        }

        private void LoadRightFiles(string folder)
        {
            if (!Directory.Exists(folder)) return;

            var leftFiles = LeftFileList.Items.Cast<string>().ToHashSet();
            var rightFiles = Directory.GetFiles(folder)
                                      .Select(f => new FileItem
                                      {
                                          Name = Path.GetFileName(f),
                                          FullPath = f,
                                          IsMatch = leftFiles.Contains(Path.GetFileName(f))
                                      }).ToList();

            RightFileList.ItemsSource = rightFiles;
        }

        private void LeftFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LeftFileList.SelectedItem is string fileName)
            {
                string fullPath = Path.Combine(initialFolder, fileName);
                TopImage.Source = LoadImage(fullPath);
            }
        }

        private void RightFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RightFileList.SelectedItem is FileItem item)
            {
                BottomImage.Source = LoadImage(item.FullPath);
            }
        }

        private void FolderSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FolderSelector.SelectedItem is string folder)
            {
                LoadRightFiles(folder);
            }
        }

        private BitmapImage LoadImage(string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public class FileItem
        {
            public string Name { get; set; }
            public string FullPath { get; set; }
            public bool IsMatch { get; set; }
        }
    }
}
