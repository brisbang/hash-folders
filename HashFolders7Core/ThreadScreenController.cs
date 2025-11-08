using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using HashLib7;

namespace HashFolders
{
    public class ThreadScreenController
    {
        private System.Windows.Threading.DispatcherTimer _timer;
        private object _mutexMessage;
        private AsyncManager Manager;
        private Window Screen;
        private IManagerWindow ScreenExtra;
        private System.Windows.Controls.Button btnAbort1;
        private System.Windows.Controls.Button btnPause;
        private System.Windows.Controls.Button btnResume;
        private System.Windows.Controls.Button btnThreadInc;
        private System.Windows.Controls.Button btnThreadDec;
        private System.Windows.Controls.Label lbState;
        private System.Windows.Controls.Label lbRemaining;
        private System.Windows.Controls.Label lbFilesOutstanding;
        private System.Windows.Controls.Label lbFilesProcessed;
        private System.Windows.Controls.Label lbNumThreadsRunning;
        private System.Windows.Controls.Label lbDuration;
        private WorkerStatusViewModel ReportViewModel;
        private ManagerStatus LastStatus;

        public ThreadScreenController(Window w,
            System.Windows.Controls.Button btnAbort1,
            System.Windows.Controls.Button btnPause,
            System.Windows.Controls.Button btnResume,
            System.Windows.Controls.Button btnThreadInc,
            System.Windows.Controls.Button btnThreadDec,
            System.Windows.Controls.Label lbState,
            System.Windows.Controls.Label lbRemaining,
            System.Windows.Controls.Label lbFilesOutstanding,
            System.Windows.Controls.Label lbFilesProcessed,
            System.Windows.Controls.Label lbNumThreadsRunning,
            System.Windows.Controls.Label lbDuration,
            WorkerStatusViewModel wsvm)
        {
            _mutexMessage = new();
            Screen = w ?? throw new InvalidOperationException("Window cannot be null");
            ScreenExtra = (IManagerWindow)Screen;
            Manager = ScreenExtra.AsyncManager;
            Screen.Closing += OnClosing;
            this.btnAbort1 = btnAbort1; this.btnAbort1.Click += btnAbort1_Click;
            this.btnPause = btnPause; this.btnPause.Click += btnPause_Click;
            this.btnResume = btnResume; this.btnResume.Click += btnResume_Click;
            this.btnThreadInc = btnThreadInc; this.btnThreadInc.Click += btnThreadInc_Click;
            this.btnThreadDec = btnThreadDec; this.btnThreadDec.Click += btnThreadDec_Click;
            this.lbState = lbState;
            this.lbRemaining = lbRemaining;
            this.lbFilesOutstanding = lbFilesOutstanding;
            this.lbFilesProcessed = lbFilesProcessed;
            this.lbNumThreadsRunning = lbNumThreadsRunning;
            this.lbDuration = lbDuration;
            this.ReportViewModel = wsvm;
            _mutexMessage = new();
            Refresh(null, null);
            _timer = new()
            {
                Interval = new TimeSpan(0, 0, 1)
            };
            _timer.Tick += Refresh;
            _timer.IsEnabled = true;
            ActivateButtonsByStatus(Manager.GetStatus());
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            //Try to end it in case it's still running.
            try
            {
                Manager.Abort();
            }
            catch { }
        }

        public void Refresh(object sender, EventArgs e)
        {
            try
            {
                ManagerStatus status = Manager.GetStatus();
                ScreenExtra.RefreshStats(status);
                lbRemaining.Content = "";
                ActivateButtonsByStatus(status);
                lbState.Content = status.state.ToString();
                lbFilesOutstanding.Content = status.filesOutstanding;
                lbFilesProcessed.Content = status.filesProcessed;
                lbNumThreadsRunning.Content = status.threadCount;
                lbDuration.Content = status.duration.ToString("hh\\:mm\\:ss");
                ShowWorkerStatuses(ReportViewModel, status.workerStatuses);
            }
            catch (Exception ex)
            {
                lock (_mutexMessage)
                {
                    MessageBox.Show(ex.ToString());
                }
                Screen.Close();
            }
        }

        private void ShowWorkerStatuses(WorkerStatusViewModel wsvm, List<WorkerStatus> workerStatuses)
        {
            int rowIndex = 0;
            foreach (WorkerStatus ws in workerStatuses)
            {
                RowItem row;
                bool isNew = false;
                if (rowIndex < wsvm.Rows.Count)
                    row = wsvm.Rows[rowIndex];
                else
                {
                    row = new();
                    isNew = true;
                }
                row.Action = ws.Action;
                row.Target = ws.Target;
                if (isNew)
                    wsvm.Rows.Add(row);
                rowIndex++;
            }
            while (wsvm.Rows.Count > rowIndex)
                wsvm.Rows.RemoveAt(wsvm.Rows.Count - 1);
            
        }

        private void ActivateButtonsByStatus(ManagerStatus status)
        {
            switch (status.state)
            {
                case StateEnum.Running:
                    btnAbort1.IsEnabled = true;
                    btnPause.IsEnabled = true;
                    btnResume.IsEnabled = false;
                    btnThreadInc.IsEnabled = true;
                    btnThreadDec.IsEnabled = (Manager.NumThreadsDesired > 1);
                    break;
                case StateEnum.Paused:
                    btnAbort1.IsEnabled = true;
                    btnPause.IsEnabled = false;
                    btnResume.IsEnabled = true;
                    btnThreadInc.IsEnabled = true;
                    btnThreadDec.IsEnabled = (Manager.NumThreadsDesired > 1);
                    break;
                case StateEnum.Stopping:
                case StateEnum.Stopped:
                case StateEnum.Undefined:
                    btnAbort1.IsEnabled = false;
                    btnPause.IsEnabled = false;
                    btnResume.IsEnabled = false;
                    btnThreadInc.IsEnabled = false;
                    btnThreadDec.IsEnabled = false;
                    break;
                default: throw new InvalidOperationException("Undefined StateEnum");
            }
            LastStatus = status;
        }

        private void btnThreadInc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Manager.ThreadInc();
                ActivateButtonsByStatus(LastStatus);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "Thread increment", MessageBoxButton.OK, MessageBoxImage.Error);
             }
        }

        private void btnThreadDec_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Manager.ThreadDec();
                ActivateButtonsByStatus(LastStatus);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "Thread decrement", MessageBoxButton.OK, MessageBoxImage.Error);
             }
        }

        private void btnAbort1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Manager.Abort();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Abort", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Manager.Pause();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Pause", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnResume_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Manager.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Resume", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
