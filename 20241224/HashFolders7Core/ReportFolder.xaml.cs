using HashLib7;
using System;
using System.Windows;

namespace HashFolders
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ReportFolder : Window
    {
        private ReportManager _activeReporter;

        public ReportFolder()
        {
            InitializeComponent();
            tbFolder.Text = HashLib7.UserSettings.RecentlyUsedFolder;
            tbNumThreads.Text = HashLib7.UserSettings.ReportThreadCount.ToString();
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _activeReporter = new ReportManager();
                _activeReporter.ExecuteAsync(tbFolder.Text, int.Parse(tbNumThreads.Text));
                ReportProcessing p = new(_activeReporter);
                p.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Report away");
            }
        }

        private void btnHash_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
