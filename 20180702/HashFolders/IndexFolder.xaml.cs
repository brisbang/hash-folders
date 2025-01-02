using HashLib2;
using System;
using System.Windows;

namespace HashFolders
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class IndexFolder : Window
    {
        private Hasher _activeHasher;
//        private System.Windows.Threading.DispatcherTimer _activeTimer;

        public IndexFolder()
        {
            InitializeComponent();
//            _activeTimer = new System.Windows.Threading.DispatcherTimer();
            _activeHasher = new Hasher();
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnHash_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _activeHasher.ExecuteAsync(tbFolder.Text, tbDestination.Text, int.Parse(tbNumThreads.Text));
                Processing p = new Processing(_activeHasher);
                p.ShowDialog();
                /*
                btnHash.IsEnabled = false;
                lbFilesOutstanding.Content = "0";
                lbFilesProcessed.Content = "0";
                lbFoldersOutstanding.Content = "1";
                lbFoldersProcessed.Content = "0";
                _activeHasher.Folder = tbFolder.Text;
                _activeHasher.Destination = tbDestination.Text;
                if (_activeHasher != null)
                {
                    _activeTimer.IsEnabled = false;
                }
                else
                {
                    _activeTimer = new System.Windows.Threading.DispatcherTimer();
                }
                _activeTimer.Interval = TimeSpan.FromSeconds(1);
                _activeTimer.Tick += _timer_Tick;
                _activeHasher.ExecuteAsync();
                _activeTimer.IsEnabled = true;
                btnAbort1.IsAncestorOf = true;
                */
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Hash away");
                btnHash.IsEnabled = true;
            }
        }

/*        private void _timer_Tick(object sender, EventArgs e)
        {
            try
            {
                bool isComplete = _activeHasher.GetStatistics(out int numFilesProcessed, out int numFoldersProcessed, out int numFilesOutstanding, out int numFoldersOutstanding, out int numThreadsRunning);
                lbFilesOutstanding.Content = numFilesOutstanding;
                lbFilesProcessed.Content = numFilesProcessed;
                lbFoldersOutstanding.Content = numFoldersOutstanding;
                lbFoldersProcessed.Content = numFoldersProcessed;
                lblNumThreadsRunning.Content = numThreadsRunning;
                if (isComplete)
                {
                    _activeTimer.IsEnabled = false;
                    btnAbort1.IsEnabled = false;
                    btnHash.IsEnabled = true;
                }
            }
            catch { }
        }

        private void btnAbort1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnAbort1.IsEnabled = false;
                btnHash.IsEnabled = true;
                _activeHasher.Abort();
            }
            catch { }
        }
*/
    }
}
