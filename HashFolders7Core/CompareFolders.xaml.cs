using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using HashLib7;

//This task is about:
//  Eliminating folders which are clearly old versions
//  It isn't really about proving the files are backed up
//  So if there's a duplicate in the same folder, there won't be a colour coding
//  If there are lots of copies of an individual file, we won't pointlessly show a whole bunch of folders

//When I select a folder, I want:
//  Targeted rescan of that specific folder
//  For each file with size > 0
//    Find files that are equivalent by hash but are located in other folders
//    Where the number of matches is <= 10, store the folder match (distinctly)
//  Rescan those folders
//  For each file
//    Find files that are equivalent by hash but are in other folders
//    If the matching file belongs within a different folder to the scan folder
//      If the number of distinct folder matches > 10
//        Flag that file as having multiple matches and don't store a folder match
//      Else
//        Flag that file as having a match
//        For each file match
//          Add the folder distinctly
//          Add the file to that folder's list of matches
//          Add the hash distinctly to that folder

//The number of matches for a folder is the count of hashes. It shows the number of distinct files that are in common.
//The problem being solved here is if there are N copies in the source and target folder, then each copy will compare multiple times to the target and you'll get N^2 matches

//On the left we have the list of files
//Each file has the number of duplicates in parentheses
//Each file goes green if there is an active match on the right
//Each file should go yellow if there are more than 10 matches

//On the right we have a list of folders with files that contain one or more matches (by hash) with files on the left.
//Files that have more than ten matches are not included in the number of matches for the folder
//The list of folders should be sorted by the number of matching hashes in descending order
//The list of folders should show the number of matching hashes in the parentheses, alongside the folder name
//When a folder is selected, the files' directory listing should be shown in the right-hand list
//Each file on the right should go green if it is flagged as a match

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
            //TODO: List of green files in LHS is wrong
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
            if (LeftFileList.SelectedItem is FileInfoDetailedComparison fileName)
            {
                TopImage.Source = LoadImage(fileName.FileInfo.FullName);
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
                BottomImage.Source = null;
            }
        }

        private static BitmapImage LoadImage(string path)
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
