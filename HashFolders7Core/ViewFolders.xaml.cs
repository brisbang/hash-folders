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
            var numBackups = info.backupLocations?.Count ?? 0;
            InfoHash.Text = $"Hash: {info.hash}";
            DisplaySize("Size", info.size, InfoSize, InfoSizeBar);
            DisplaySize("Total size", info.size * (numBackups + 1), InfoSizeAll, InfoSizeBarAll);
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

        private static void DisplaySize(string header, long size, TextBlock infoSize, ProgressBar infoSizeBar)
        {
            infoSize.Text = $"{header}: {size:N0} bytes";
            infoSizeBar.Value = GetLogSizePercent(size);
            infoSizeBar.ToolTip = $"{size:N0} bytes";
            infoSizeBar.Foreground = GetSizeGradientBrush(size);
        }

        private static double GetLogSizePercent(long size)
        {
            const double minSize = 1;               // Avoid log(0)
            const double maxSize = 1_000_000_000;     // 100MB upper bound

            double clamped = Math.Max(size, minSize);
            double logMin = Math.Log10(minSize);
            double logMax = Math.Log10(maxSize);
            double logSize = Math.Log10(clamped);

            return ((logSize - logMin) / (logMax - logMin)) * 100;
        }

        private static Brush GetSizeGradientBrush(long size)
        {
            return new SolidColorBrush(InterpolateColor(size));
        }

        private static Color InterpolateColor(long size)
        {
            const double orangeThreshold = 5_000_000;   // 5MB
            const double redThreshold = 100_000_000;    // 100MB

            if (size <= orangeThreshold)
            {
                // Green to Orange
                double t = size / orangeThreshold;
                byte r = (byte)(t * 255);     // 0 → 255
                byte g = 255;                 // stays full
                return Color.FromRgb(r, g, 0); // (r,255,0)
            }
            else if (size <= redThreshold)
            {
                // Orange to Red
                double t = (size - orangeThreshold) / (redThreshold - orangeThreshold);
                byte r = 255;                 // stays full
                byte g = (byte)((1 - t) * 255); // 255 → 0
                return Color.FromRgb(r, g, 0); // (255,g,0)
            }
            else
            {
                // Beyond red threshold — solid red
                return Color.FromRgb(255, 0, 0);
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
