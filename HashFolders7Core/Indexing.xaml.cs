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
        private IndexManager _hasher;
        private ThreadScreenController _controller;

        public Indexing(IndexManager Hasher)
        {
            InitializeComponent();
            _hasher = Hasher;
            _controller = new ThreadScreenController(this, btnAbort1, btnClose1, btnPause, btnResume, lbState);
        }

        public StateEnum Refresh(object sender, EventArgs e)
        {
            IndexStatus status = _hasher.GetStatistics();
            if (status.filesOutstanding > 0)
                lbFilesOutstanding.Content = status.filesOutstanding;
            else
                lbFilesOutstanding.Content = status.filesToDelete;
            lbFilesProcessed.Content = status.filesProcessed;
            lbFoldersOutstanding.Content = status.foldersOutstanding;
            lbFoldersProcessed.Content = status.foldersProcessed;
            lbNumThreadsRunning.Content = status.threadCount;
            lbDuration.Content = status.duration.ToString("hh\\:mm\\:ss");
            return _hasher.State;
        }
        public void Abort()
        {
            _hasher.Abort();
        }
        public void Pause()
        {
            _hasher.Suspend();
        }
        public void Resume()
        {
            _hasher.Resume();
        }
        public void CloseWindow()
        {
            this.Close();
        }


    }
}
