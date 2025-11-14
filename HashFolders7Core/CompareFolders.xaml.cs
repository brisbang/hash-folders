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
            Refresh();

        }

        private void Refresh()
        {
            FileComparisonList comparison = FileManager.GetComparisonFolders(initialFolder);

            LeftFolder.Content = initialFolder;
            LeftFileList.ItemsSource = comparison.Files;
            FolderSelector.ItemsSource = comparison.FolderCounts;
            FolderSelector.SelectedIndex = 0;
        }

        private void LoadRightFiles(string folder)
        {
            if (!Directory.Exists(folder)) return;

            HashSet<FileInfoDetailedComparison> leftFiles = (HashSet<FileInfoDetailedComparison>)LeftFileList.Items.Cast<FileInfoDetailedComparison>().ToHashSet();
            //TODO: Get the files, get the FileInfoDetailed, load it in and declare IsMatch if it matches a hash on the left - attempted
            //TODO: Show the LHS as green as required - attempted
            //TODO: Order the list of file paths in descending order
            //TODO: Left list box is not scrolling
            //TODO: Left image is not displaying
            ResetLeftFilesView(leftFiles);
            var rightFiles = Directory.GetFiles(folder)
                                      .Select(f => new FileItem
                                      {
                                          Name = Path.GetFileName(f),
                                          FullPath = f,
                                          IsMatch = fidcContainsHash(leftFiles, f)
                                      }).ToList();

            RightFileList.ItemsSource = rightFiles;
        }

        private static void ResetLeftFilesView(HashSet<FileInfoDetailedComparison> leftFiles)
        {
            foreach (FileInfoDetailedComparison fidc in leftFiles)
                fidc.HasSpecificMatch = false;
        }

        private bool fidcContainsHash(HashSet<FileInfoDetailedComparison> list, string filePath)
        {
            FileInfoDetailed fid = FileManager.RetrieveFile(new PathFormatted(filePath));
            bool match = false;
            if (fid == null)
                return false;
            foreach (FileInfoDetailedComparison fidc in list)
            {
                if (fidc.FileInfo.Hash == fid.Hash)
                {
                    match = true;
                    fidc.HasSpecificMatch = true;
                }
            }
            return match;
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
