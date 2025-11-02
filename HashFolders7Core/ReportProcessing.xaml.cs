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
            _controller = new ThreadScreenController(this, btnAbort1, btnClose1, btnPause, btnResume, lbState);
        }

        public StateEnum Refresh(object sender, EventArgs e)
        {
            ReportStatus status = _reporter.GetStatus();
            lbFilesOutstanding.Content = status.fileCount - status.filesProcessed;
            lbFilesProcessed.Content = status.filesProcessed;
            lbNumThreadsRunning.Content = status.threadCount;
            lbDuration.Content = status.duration.ToString("hh\\:mm\\:ss");
            if (status.state == StateEnum.Running)
                lbRemaining.Content = status.timeRemaining.ToLongTimeString();
            lbResultFile.Content = status.outputFile;
            return status.state;
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
