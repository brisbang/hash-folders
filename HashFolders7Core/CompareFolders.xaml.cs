using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using HashLib7;

namespace HashFolders
{
    public partial class CompareFolders : Window
    {
        private string initialFolder;

        public CompareFolders(string folder)
        {
            InitializeComponent();
            initialFolder = folder;

            FileComparisonList comparison = FileManager.GetComparisonFolders(folder);

            LeftFolder.Content = initialFolder;
            LeftFileList.ItemsSource = comparison.Files;
            FolderSelector.ItemsSource = comparison.FolderCounts;
            FolderSelector.SelectedIndex = 0;

//            LoadRightFiles(comparison.FolderCounts[0].Folder);
        }

        private void LoadRightFiles(string folder)
        {
            if (!Directory.Exists(folder)) return;

            var leftFiles = LeftFileList.Items.Cast<FileInfoDetailed>().ToHashSet();
            //TODO: Get the files, get the FileInfoDetailed, load it in and declare IsMatch if it matches a hash on the left.
            //TODO: Show the LHS as green as required
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
            if (FolderSelector.SelectedItem is FolderCount folder)
            {
                LoadRightFiles(folder.Folder);
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
