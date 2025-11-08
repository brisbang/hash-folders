using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HashLib7;
using Microsoft.Graph.Models;
using Microsoft.Graph.Solutions.BackupRestore;

namespace HashFolders
{
    public partial class ViewFolders : Window
    {
        private class BarValue
        {
            public TextBlock Label { get; set; }
            public long MaxSize { get; set; }
        }
        private readonly List<BarValue> sizeBars;
        private readonly List<BarValue> sizeBackupBars;
        private string targetFilePath;

        public ViewFolders()
        {
            InitializeComponent();
            SetConfig();
            HideFileDetail();

            targetFilePath = HashLib7.UserSettings.RecentlyUsedFolder;
            var rootFolders = new List<FolderItem>();
            FolderTree.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(FolderTree_Expanded));
            foreach (HashLib7.DriveInfo drive in Config.Drives.GetAll())
                rootFolders.Add(new FolderItem() { Path = drive.Letter + ":\\" });
            FolderTree.ItemsSource = rootFolders;
            FolderTree.Loaded += FolderTree_Loaded;
            sizeBars =
                [
                new() {Label = InfoSize0, MaxSize=0},
                new() {Label = InfoSize100K, MaxSize=100_000},
                new() {Label = InfoSize1M, MaxSize=1_000_000},
                new() {Label = InfoSize10M, MaxSize=10_000_000},
                new() {Label = InfoSize50M, MaxSize=50_000_000},
                new() {Label = InfoSize100M, MaxSize=100_000_000},
                new() {Label = InfoSize500M, MaxSize=500_000_000},
                new() {Label = InfoSize1G, MaxSize=1_000_000_000},
                new() {Label = InfoSizeLarge, MaxSize=long.MaxValue}
                ];

            sizeBackupBars =
                [
                new() {Label = BackupSize0, MaxSize=0},
                new() {Label = BackupSize100K, MaxSize=100_000},
                new() {Label = BackupSize1M, MaxSize=1_000_000},
                new() {Label = BackupSize10M, MaxSize=10_000_000},
                new() {Label = BackupSize50M, MaxSize=50_000_000},
                new() {Label = BackupSize100M, MaxSize=100_000_000},
                new() {Label = BackupSize500M, MaxSize=500_000_000},
                new() {Label = BackupSize1G, MaxSize=1_000_000_000},
                new() {Label = BackupSizeLarge, MaxSize=long.MaxValue}
                ];
        }

        private static void SetConfig()
        {
            string dataPath = System.Configuration.ConfigurationManager.AppSettings["dataPath"];
            string connStr = System.Configuration.ConfigurationManager.AppSettings["connectionString"];
            string debug = System.Configuration.ConfigurationManager.AppSettings["logDebug"];
            string[] drives = System.Configuration.ConfigurationManager.AppSettings["drives"].Split(',');
            DriveInfoList driveInfos = LoadDriveInfos(drives);
            Config.SetParameters(App.ServiceProvider, dataPath, connStr, driveInfos, debug == "true");
        }

        private static DriveInfoList LoadDriveInfos(string[] drives)
        {
            DriveInfoList res = new();
            foreach (string drive in drives)
            {
                try
                {
                    string[] parts = drive.Split('|');
                    res.Add(new HashLib7.DriveInfo(parts[0][0], int.Parse(parts[1]), parts[2]));
                }
                catch { }
            }
            
            return res;
        }

        private void FolderTree_Loaded(object sender, RoutedEventArgs e)
        {
            SelectFolderPath(targetFilePath);
        }

        private void FolderTree_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem item && item.DataContext is FolderItem folder)
            {
                folder.LoadSubFolders();
            }
        }

        public void SelectFolderPath(string fullPath)
        {
            TraverseLevel(FolderTree);
        }

        private void TraverseLevel(ItemsControl parent)
        {
            bool found = false;
            if (targetFilePath == null)
                return;
            foreach (var item in parent.Items)
            {
                if (item is FolderItem folder &&
                    targetFilePath.StartsWith(folder.Path, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    var container = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

                    if (container == null)
                    {
                        parent.UpdateLayout();
                        container = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                    }

                    if (container != null)
                    {
                        if (targetFilePath.Equals(folder.Path, StringComparison.OrdinalIgnoreCase))
                        {
                            container.IsSelected = true;
                            container.BringIntoView();
                            targetFilePath = null;
                        }
                        else
                        {
                            WaitForChildContainer(container);
                            container.IsExpanded = true;
                        }
                    }

                    break;
                }
            }
            if (!found)
            {
                targetFilePath = null;
            }
        }

        private void WaitForChildContainer(TreeViewItem parentContainer)
        {
            EventHandler handler = null;
            handler = (sender, args) =>
            {
                if (parentContainer.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                {
                    parentContainer.ItemContainerGenerator.StatusChanged -= handler;

                    TraverseLevel(parentContainer);
                }
            };

            parentContainer.ItemContainerGenerator.StatusChanged += handler;
            parentContainer.UpdateLayout();
        }


        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (FolderTree.SelectedItem is FolderItem folder)
            {
                HashLib7.UserSettings.RecentlyUsedFolder = folder.Path;
                LoadFolderFiles(folder);
            }
        }

        private void LoadFolderFiles(FolderItem folder)
        {
            FileList.Items.Clear();
            try
            {
                foreach (var file in Directory.GetFiles(folder.Path))
                {
                    var pf = new PathFormatted(file);
                    FileList.Items.Add(pf);
                }
            }
            catch { /* Ignore access errors */ }
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PathFormatted filePath = null;
            try
            {
                filePath = FileList.SelectedItem as PathFormatted;
            }
            catch { }
            if (filePath == null)
            {
                HideFileDetail();
                return;
            }
            ShowFileDetail(filePath);
            ShowImageDetail(filePath);
        }

        private void HideFileDetail()
        {
            RightDockPanel.Visibility = Visibility.Hidden;
        }

        private void ShowImageDetail(PathFormatted filePath)
        {
            try
            {
                if (!IsImageFile(filePath.FullName))
                {
                    HideImagePreview();
                    return;
                }
                // Load image with EXIF rotation
                using var stream = new FileStream(filePath.FullName, FileMode.Open, FileAccess.Read);
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
            RightDockPanel.Visibility = Visibility.Visible;
            var info = FileManager.RetrieveFile(filePath);
            FileToolbar.Visibility = Visibility.Visible;
            InfoHash.Text = $"Hash: {info.hash}";
            DisplaySize(info.size, sizeBars);
            DisplaySize(info.backupLocations.Count * info.size, sizeBackupBars);
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
            AssessRisks(FileManager.GetRiskAssessment(filePath));

        }

        private void AssessRisks(RiskAssessment ra)
        {
            DisplayRisk(RiskDiskFailure, BorderDiskFailure, ra.DiskFailure);
            DisplayRisk(RiskCorruption, BorderCorruption, ra.Corruption);
            DisplayRisk(RiskTheft, BorderTheft, ra.Theft);
            DisplayRisk(RiskFire, BorderFire, ra.Fire);
        }

        private static void DisplayRisk(TextBlock risk, Border border, bool atRisk)
        {
            risk.Text = atRisk ? "At Risk" : "Mitigated";
            border.Background = atRisk ? Brushes.Red : Brushes.Green;
        }

        private static void DisplaySize(long size, List<BarValue> sizeBars)
        {
            bool selected = false;
            foreach (var bar in sizeBars)
            {
                if (size <= bar.MaxSize && !selected)
                {
                    SetBar(bar, true, size);
                    selected = true;
                }
                else
                    SetBar(bar, false);
            }
        }

        private static void SetBar(BarValue bar, bool highlight, long size = -1)
        {
            if (highlight)
            {
                bar.Label.ToolTip = $"{size:N0}";
                bar.Label.Background = GetSizeGradientBrush(size);
            }
            else
            {
                bar.Label.ToolTip = null;
                bar.Label.Background = Brushes.LightGray;
            }
        }

        private static double GetSizePercent(long size)
        {
            const long maxSize = 100_000_000; // 100MB
            if (size >= maxSize)
                return 100;
            return ((double)size / maxSize) * 100;
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

        private static Brush GetSizeGradientBrush(long size) => new SolidColorBrush(InterpolateColor(size));

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

        private void IndexFolder_Click(object sender, RoutedEventArgs e)
        {
            const string title = "Index";
            try
            {
                IndexManager indexer = new();
                Indexing p = new(indexer);
                LaunchScreen(indexer, p, title);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error indexing folder: {ex.Message}", title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RiskAssessment_Click(object sender, RoutedEventArgs e)
        {
            const string title = "Risk Assessment";
            try
            {
                RAManager assessor = new();
                RiskAssessmentProcessing p = new(assessor);
                LaunchScreen(assessor, p, title);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reporting folder: {ex.Message}", title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReportFolder_Click(object sender, RoutedEventArgs e)
        {
            const string title = "Report";
            try
            {
                ReportManager reporter = new();
                ReportProcessing p = new(reporter);
                LaunchScreen(reporter, p, title);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reporting folder: {ex.Message}", title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LaunchScreen(AsyncManager mgr, Window w, string title)
        {
            if (FolderTree.SelectedItem is not FolderItem folder)
            {
                MessageBox.Show("Please select a folder", title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            mgr.ExecuteAsync(folder.Path, HashLib7.UserSettings.ThreadCount);
            w.ShowDialog();
        }

        private void HandleDeleteAction()
        {
            PathFormatted filePath;
            try
            {
                filePath = FileList.SelectedItem as PathFormatted;
            }
            catch
            {
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
