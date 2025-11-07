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
            _controller = new ThreadScreenController(this, btnAbort1, btnClose1, btnPause, btnResume, lbState, lbFilesProcessed, lbFilesOutstanding, lbFilesProcessed, lbNumThreadsRunning, lbDuration);
        }

        public TaskStatus Refresh(object sender, EventArgs e)
        {
            IndexStatus status = (IndexStatus) _hasher.GetStatus();
            lbFoldersOutstanding.Content = status.foldersOutstanding;
            lbFoldersProcessed.Content = status.foldersProcessed;

            return status;
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
