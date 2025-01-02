using HashLib3;
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
                _hasher.GetStatistics(out int numFilesProcessed, out int numFoldersProcessed, out int numFilesOutstanding, out int numFoldersOutstanding, out int numThreadsRunning);
                lbFilesOutstanding.Content = numFilesOutstanding;
                lbFilesProcessed.Content = numFilesProcessed;
                lbFoldersOutstanding.Content = numFoldersOutstanding;
                lbFoldersProcessed.Content = numFoldersProcessed;
                lbNumThreadsRunning.Content = numThreadsRunning;
                lbDuration.Content = _timespan.ToString();
                HashLib3.Hasher.StateEnum state = _hasher.State;
                lbState.Content = _hasher.State.ToString();
                switch (state)
                {
                    case Hasher.StateEnum.Aborting:
                        btnAbort1.IsEnabled = false;
                        btnClose1.IsEnabled = false;
                        btnPause.IsEnabled = false;
                        btnResume.IsEnabled = false;
                        break;
                    case Hasher.StateEnum.Running:
                        btnAbort1.IsEnabled = true;
                        btnClose1.IsEnabled = false;
                        btnPause.IsEnabled = true;
                        btnResume.IsEnabled = false;
                        break;
                    case Hasher.StateEnum.Stopped:
                        btnAbort1.IsEnabled = false;
                        btnClose1.IsEnabled = true;
                        btnPause.IsEnabled = false;
                        btnResume.IsEnabled = false;
                        break;
                    case Hasher.StateEnum.Suspended:
                        btnAbort1.IsEnabled = true;
                        btnClose1.IsEnabled = false;
                        btnPause.IsEnabled = false;
                        btnResume.IsEnabled = true;
                        break;
                    default:
                        btnAbort1.IsEnabled = false;
                        btnClose1.IsEnabled = false;
                        btnPause.IsEnabled = false;
                        btnResume.IsEnabled = false;
                        break;
                }
                if (state == Hasher.StateEnum.Stopped)
                    _timer.Stop();
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
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _hasher.Suspend();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnResume_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _hasher.Resume();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

    }
}
