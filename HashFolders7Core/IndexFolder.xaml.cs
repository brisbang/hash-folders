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
        private string _folder;

        public IndexFolder(string folder)
        {
            InitializeComponent();
            _folder = folder;
            tbNumThreads.Text = HashLib7.UserSettings.ThreadCount.ToString();
        }

        private void btnHash_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _activeHasher = new ThreadManager();
                //_activeHasher.ExecuteAsync(tbFolder.Text, int.Parse(tbNumThreads.Text), tbOneDriveUser.Text, tbOneDrivePassword.Text);
                _activeHasher.ExecuteAsync(_folder, int.Parse(tbNumThreads.Text));
                Processing p = new(_activeHasher);
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
