using HashLib7;
using System;
using System.Windows;

namespace HashFolders
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Indexing : Window, IThreadScreen
    {
        private IndexManager Indexer;
        private ThreadScreenController _controller;

        public Indexing(IndexManager indexer)
        {
            InitializeComponent();
            Indexer = indexer;
            DataContext = new WorkerStatusViewModel();
            _controller = new ThreadScreenController(this, btnAbort1, btnClose1, btnPause, btnResume, lbState, lbFilesProcessed, lbFilesOutstanding, lbFilesProcessed, lbNumThreadsRunning, lbDuration, (WorkerStatusViewModel)DataContext);
        }

        public ManagerStatus Refresh(object sender, EventArgs e)
        {
            IndexManagerStatus status = (IndexManagerStatus) Indexer.GetStatus();
            lbFoldersOutstanding.Content = status.foldersOutstanding;
            lbFoldersProcessed.Content = status.foldersProcessed;

            return status;
        }
        public void Abort()
        {
            Indexer.Abort();
        }
        public void Pause()
        {
            Indexer.Suspend();
        }
        public void Resume()
        {
            Indexer.Resume();
        }
        public void CloseWindow()
        {
            this.Close();
        }


    }
}
