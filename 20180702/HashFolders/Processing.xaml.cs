using HashLib2;
using System;
using System.Windows;

namespace HashFolders
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Processing : Window
    {
        private Hasher _hasher;
        private System.Windows.Threading.DispatcherTimer _timer;
        private TimeSpan _timespan;
        private object _mutexMessage;

        public Processing(Hasher Hasher)
        {
            InitializeComponent();
            _hasher = Hasher;
            _mutexMessage = new object();
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 1);
            _timer.Tick += Refresh;
            _timer.IsEnabled = true;
        }

        public void Refresh(object sender, EventArgs e)
        {
            try
            { 
                _timespan += _timer.Interval;
                bool isComplete = _hasher.GetStatistics(out int numFilesProcessed, out int numFoldersProcessed, out int numFilesOutstanding, out int numFoldersOutstanding, out int numThreadsRunning, out int libraryCount);
                lbFilesOutstanding.Content = numFilesOutstanding;
                lbFilesProcessed.Content = numFilesProcessed;
                lbFoldersOutstanding.Content = numFoldersOutstanding;
                lbFoldersProcessed.Content = numFoldersProcessed;
                lbNumThreadsRunning.Content = numThreadsRunning;
                lbDuration.Content = _timespan.ToString();
                lbLibrary.Content = libraryCount.ToString();
                if (isComplete)
                {
                    _timer.Stop();
                    btnClose1.IsEnabled = true;
                    btnAbort1.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                lock (_mutexMessage)
                {
                    MessageBox.Show(ex.ToString());
                }
                Close();
            }
        }

        private void btnClose1_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnAbort1_Click(object sender, RoutedEventArgs e)
        {
            _hasher.Abort();
            Close();
        }

    }
}
