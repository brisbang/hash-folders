using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace HashFolders
{
    public partial class FolderExplorer : Window
    {
        public FolderExplorer()
        {
            InitializeComponent();
            LoadFolderTree("C:\\");
        }

        private void LoadFolderTree(string rootPath)
        {
            var root = new FolderItem { Path = rootPath };
            LoadSubFolders(root);
            FolderTree.ItemsSource = new List<FolderItem> { root };
        }

        private void LoadSubFolders(FolderItem parent)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(parent.Path))
                {
                    var sub = new FolderItem { Path = dir };
                    parent.SubFolders.Add(sub);
                }
            }
            catch { /* Ignore access errors */ }
        }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (FolderTree.SelectedItem is TreeViewItem item)
            {
                string path = item.Tag.ToString();
                FileList.Items.Clear();
                try
                {
                    foreach (var file in Directory.GetFiles(path))
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