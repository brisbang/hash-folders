using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HashLib7;

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
                    // Load image with EXIF rotation
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    var frame = decoder.Frames[0];
                    ImagePreview.Source = ApplyExifRotation(frame);

                    // Load summary info
                    var info = FileManager.RetrieveFile(filePath);
//                    InfoPath.Text = $"Path: {info.path}";
                    InfoSize.Text = $"Size: {info.size:N0} bytes";
//                    InfoFilename.Text = $"Filename: {info.filename}";
                    InfoHash.Text = $"Hash: {info.hash}";
                    StringBuilder sb = new();
                    foreach (var fp in info.backupLocations)
                    {
                        if (sb.Length > 0) sb.Append(", ");
                        sb.Append(fp.ToString());
                    }
                    InfoBackups.Text = $"Backups: {sb.ToString()}";
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

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string action = btn.Content.ToString();

                if (action == "Delete")
                {
                    HandleDeleteAction();
                }
                else
                {
                    MessageBox.Show($"Action triggered: {action}", "Image Action", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }


        private void HandleDeleteAction()
        {
            if (FileList.SelectedItem is string filePath)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {

                    try
                    {
                        HashLib7.FileManager.DeleteFile(filePath);
                        // Remove from file list
                        FileList.Items.Remove(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting file: {ex.Message}", "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("To delete this file hold down the shift key and click again", "Delete Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private static BitmapSource ApplyExifRotation(BitmapFrame frame)
        {
            if (frame.Metadata is BitmapMetadata metadata &&
                metadata.ContainsQuery("System.Photo.Orientation"))
            {
                object orientation = metadata.GetQuery("System.Photo.Orientation");
                if (orientation is ushort value)
                {
                    var transform = new TransformedBitmap(frame, new RotateTransform(value switch
                    {
                        6 => 90,
                        3 => 180,
                        8 => 270,
                        _ => 0
                    }));
                    return transform;
                }
            }

            return frame;
        }

        private static bool IsImageFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif";
        }
    }
}
