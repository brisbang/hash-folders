using HashLib7;
using System;
using System.Diagnostics;
using System.Windows;

namespace HashFolders
{
    /// <summary>
    /// Interaction logic for ReportProcessing.xaml
    /// </summary>
    public partial class ReportProcessing : Window, IManagerWindow
    {
        private ReportManager _reporter;
        private ThreadScreenController _controller;

        public ReportProcessing(ReportManager reporter)
        {
            InitializeComponent();
            _reporter = reporter;
            // set up a simple ViewModel for the ListView
            DataContext = new WorkerStatusViewModel();
            _controller = new ThreadScreenController(this, btnAbort1, btnPause, btnResume, btnThreadInc, btnThreadDec, lbState, lbFilesProcessed, lbFilesOutstanding, lbFilesProcessed, lbNumThreadsRunning, lbDuration, (WorkerStatusViewModel)DataContext);
            this.btnOpenResult.Click += btnOpenResult_Click;
        }

        public AsyncManager AsyncManager => _reporter;

        public void RefreshStats(ManagerStatus mgrStatus)
        {
            ReportManagerStatus status = (ReportManagerStatus)mgrStatus;
            lbResultFile.Content = status.outputFile;
            btnOpenResult.IsEnabled = (mgrStatus.state == StateEnum.Stopped);
/*
            if (status.state == StateEnum.Running)
                lbRemaining.Content = status.timeRemaining.ToLongTimeString();
            else
                lbRemaining.Content = "";
                */
        }

        private void btnOpenResult_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _reporter.OutputFile,
                    UseShellExecute = true // Required to use default app
                });
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "Open", MessageBoxButton.OK, MessageBoxImage.Error);
             }
        }


    }
}
