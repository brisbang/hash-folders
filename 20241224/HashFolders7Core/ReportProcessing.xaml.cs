using HashLib7;
using System;
using System.Windows;

namespace HashFolders
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ReportProcessing : Window
    {
        private ReportManager _reporter;
        private System.Windows.Threading.DispatcherTimer _timer;
        private object _mutexMessage;

        public ReportProcessing(ReportManager reporter)
        {
            InitializeComponent();
            _reporter = reporter;
            _mutexMessage = new();
            _timer = new()
            {
                Interval = new TimeSpan(0, 0, 1)
            };
            _timer.Tick += Refresh;
            _timer.IsEnabled = true;
        }

        public void Refresh(object sender, EventArgs e)
        {
            try
            { 
                ReportStatus status = _reporter.GetStatus();
                lbFilesOutstanding.Content = status.fileCount - status.filesProcessed;
                lbFilesProcessed.Content = status.filesProcessed;
                lbNumThreadsRunning.Content = status.threadCount;
                lbDuration.Content = new DateTime((DateTime.Now - status.startTime).Ticks).ToLongTimeString();
                if (status.state == StateEnum.Running)
                    lbRemaining.Content = status.timeRemaining.ToLongTimeString();
                lbState.Content = status.state.ToString();
                lbResultFile.Content = status.outputFile;
                switch (status.state)
                {
                    case StateEnum.Aborting:
                        btnAbort1.IsEnabled = false;
                        btnClose1.IsEnabled = false;
                        btnPause.IsEnabled = false;
                        btnResume.IsEnabled = false;
                        break;
                    case StateEnum.Running:
                        btnAbort1.IsEnabled = true;
                        btnClose1.IsEnabled = false;
                        btnPause.IsEnabled = true;
                        btnResume.IsEnabled = false;
                        break;
                    case StateEnum.Stopped:
                        btnAbort1.IsEnabled = false;
                        btnClose1.IsEnabled = true;
                        btnPause.IsEnabled = false;
                        btnResume.IsEnabled = false;
                        break;
                    case StateEnum.Suspended:
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
                if (status.state == StateEnum.Stopped)
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
            _reporter.Abort();
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _reporter.Suspend();
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
                _reporter.Resume();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

    }
}
