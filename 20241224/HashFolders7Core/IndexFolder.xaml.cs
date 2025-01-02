using HashLib7;
using System;
using System.Windows;

namespace HashFolders
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class IndexFolder : Window
    {
        private ThreadManager _activeHasher;

        public IndexFolder()
        {
            InitializeComponent();
            tbFolder.Text = HashLib7.UserSettings.RecentlyUsedFolder;
            tbNumThreads.Text = HashLib7.UserSettings.ThreadCount.ToString();
        }

        private void btnHash_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _activeHasher = new ThreadManager();
                _activeHasher.ExecuteAsync(tbFolder.Text, int.Parse(tbNumThreads.Text), tbOneDriveUser.Text, tbOneDrivePassword.Text);
                Processing p = new Processing(_activeHasher);
                p.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Hash away");
                btnHash.IsEnabled = true;
            }
        }

    }
}
