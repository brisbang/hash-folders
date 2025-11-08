using HashLib7;
using System;
using System.Windows;

namespace HashFolders
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Indexing : Window, IManagerWindow
    {
        private IndexManager Indexer;
        private ThreadScreenController _controller;

        public AsyncManager AsyncManager => Indexer;

        public Indexing(IndexManager indexer)
        {
            InitializeComponent();
            Indexer = indexer;
            DataContext = new WorkerStatusViewModel();
            _controller = new ThreadScreenController(this, btnAbort1, btnPause, btnResume, btnThreadInc, btnThreadDec, lbState, lbFilesProcessed, lbFilesOutstanding, lbFilesProcessed, lbNumThreadsRunning, lbDuration, (WorkerStatusViewModel)DataContext);
        }

        public void RefreshStats(ManagerStatus mgrStatus)
        {
            IndexManagerStatus status = (IndexManagerStatus)mgrStatus;
            lbFoldersOutstanding.Content = status.foldersOutstanding;
            lbFoldersProcessed.Content = status.foldersProcessed;
        }
        
    }
}
