using HashLib7;
using System;
using System.Windows;

namespace HashFolders
{
    /// <summary>
    /// Interaction logic for ReportProcessing.xaml
    /// </summary>
    public partial class ReportProcessing : Window, IThreadScreen
    {
        private ReportManager _reporter;
        private ThreadScreenController _controller;

        public ReportProcessing(ReportManager reporter)
        {
            InitializeComponent();
            _reporter = reporter;
            // set up a simple ViewModel for the ListView
            DataContext = new WorkerStatusViewModel();
            _controller = new ThreadScreenController(this, btnAbort1, btnClose1, btnPause, btnResume, lbState, lbFilesProcessed, lbFilesOutstanding, lbFilesProcessed, lbNumThreadsRunning, lbDuration, (WorkerStatusViewModel)DataContext);
        }

        public ManagerStatus Refresh(object sender, EventArgs e)
        {
            ReportManagerStatus status = (ReportManagerStatus) _reporter.GetStatus();
            lbResultFile.Content = status.outputFile;
/*
            if (status.state == StateEnum.Running)
                lbRemaining.Content = status.timeRemaining.ToLongTimeString();
            else
                lbRemaining.Content = "";
                */
            return status;
        }

        public void Abort()
        {
            _reporter.Abort();
        }
        public void Pause()
        {
            _reporter.Suspend();
        }
        public void Resume()
        {
            _reporter.Resume();
        }
        public void CloseWindow()
        {
            this.Close();
        }


    }
}
