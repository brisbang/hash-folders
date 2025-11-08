using HashLib7;
using System;
using System.Windows;

namespace HashFolders
{
    /// <summary>
    /// This screen refreshes the risk assessments stored in the database, allowing for external queries or quick viewing of folder information.
    /// </summary>
    public partial class RiskAssessmentProcessing : Window, IManagerWindow
    {
        private RAManager _assessor;
        private ThreadScreenController _controller;

        public RiskAssessmentProcessing(RAManager assessor)
        {
            InitializeComponent();
            _assessor = assessor;
            // set up a simple ViewModel for the ListView
            DataContext = new WorkerStatusViewModel();
            _controller = new ThreadScreenController(this, btnAbort1, btnPause, btnResume, btnThreadInc, btnThreadDec, lbState, lbFilesProcessed, lbFilesOutstanding, lbFilesProcessed, lbNumThreadsRunning, lbDuration, (WorkerStatusViewModel)DataContext);
        }

        public AsyncManager AsyncManager => _assessor;

        public void RefreshStats(ManagerStatus mgrStatus)
        {
        }
    }
}
