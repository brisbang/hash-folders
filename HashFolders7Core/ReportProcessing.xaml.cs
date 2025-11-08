using HashLib7;
using System;
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
        }

        public AsyncManager AsyncManager => _reporter;

        public void RefreshStats(ManagerStatus mgrStatus)
        {
            ReportManagerStatus status = (ReportManagerStatus)mgrStatus;
            lbResultFile.Content = status.outputFile;
/*
            if (status.state == StateEnum.Running)
                lbRemaining.Content = status.timeRemaining.ToLongTimeString();
            else
                lbRemaining.Content = "";
                */
        }


    }
}
