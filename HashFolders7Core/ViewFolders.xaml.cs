using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
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


            var rootFolders = new List<FolderItem>();
            foreach (string drive in Config.Drives)
                rootFolders.Add(new FolderItem() { Path = drive + ":\\" });
            FolderTree.ItemsSource = rootFolders;
            FolderTree.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(FolderTree_Expanded));
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
                        FileList.Items.Add(new PathFormatted(file));
                    }
                }
                catch { /* Ignore access errors */ }
            }
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PathFormatted filePath;
            try
            {
                filePath = FileList.SelectedItem as PathFormatted;
            }
            catch
            {
                HideImagePreview();
                return;
            }
            ShowImageDetail(filePath);
            ShowFileDetail(filePath);
        }

        private void ShowImageDetail(PathFormatted filePath)
        {
            try
            {
                if (!IsImageFile(filePath.fullName))
                {
                    HideImagePreview();
                    return;
                }
                // Load image with EXIF rotation
                using var stream = new FileStream(filePath.fullName, FileMode.Open, FileAccess.Read);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                var frame = decoder.Frames[0];
                ImagePreview.Source = ApplyExifRotation(frame);
                ImageUnavailableMessage.Visibility = Visibility.Collapsed;
            }
            catch
            {
                HideImagePreview();
            }
        }

        private void ShowFileDetail(PathFormatted filePath)
        {
            // Load summary info
            var info = FileManager.RetrieveFile(filePath);
            InfoSize.Text = $"Size: {info.size:N0} bytes";
            InfoHash.Text = $"Hash: {info.hash}";
            if (info.size == 0)
            {
                BackupExpander.Header = "Backups - File is empty";
                BackupList.ItemsSource = null;
            }
            else
            {
                BackupExpander.Header = $"Backups - ({info.backupLocations.Count})";
                BackupList.ItemsSource = info.backupLocations;
            }
        }

        private void HideImagePreview()
        {
            ImagePreview.Source = null;
            ImageUnavailableMessage.Visibility = Visibility.Visible;
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
            PathFormatted filePath; 
            try
            {
                filePath = FileList.SelectedItem as PathFormatted;
            }
            catch {
                return;
            }
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
