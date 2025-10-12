using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace HashFolders
{
    public partial class ViewFolders : Window
    {
        public ViewFolders()
        {
            InitializeComponent();
            DriveSelector.SelectedIndex = 0; // Default to C:\
            FolderTree.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(FolderTree_Expanded));
        }

        private void DriveSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DriveSelector.SelectedItem is ComboBoxItem selected &&
                selected.Content is string drive)
            {
                LoadRootFolder(drive);
            }
        }

        private void LoadRootFolder(string rootPath)
        {
            var root = new FolderItem { Path = rootPath };
            FolderTree.ItemsSource = new List<FolderItem> { root };
        }

        private void FolderTree_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem item && item.DataContext is FolderItem folder)
            {
                folder.LoadSubFolders();
            }
        }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (FolderTree.SelectedItem is FolderItem folder)
            {
                FileList.Items.Clear();
                try
                {
                    foreach (var file in Directory.GetFiles(folder.Path))
                    {
                        FileList.Items.Add(file);
                    }
                }
                catch { /* Ignore access errors */ }
            }
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileList.SelectedItem is string filePath && IsImageFile(filePath))
            {
                try
                {
                    ImagePreview.Source = new BitmapImage(new Uri(filePath));
                }
                catch
                {
                    ImagePreview.Source = null;
                }
            }
            else
            {
                ImagePreview.Source = null;
            }
        }

        private static bool IsImageFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif";
        }
    }
}
