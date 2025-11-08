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
        private IThreadScreen _screen;
        private System.Windows.Controls.Button btnAbort1;
        private System.Windows.Controls.Button btnClose1;
        private System.Windows.Controls.Button btnPause;
        private System.Windows.Controls.Button btnResume;
        private System.Windows.Controls.Label lbState;
        private System.Windows.Controls.Label lbRemaining;
        private System.Windows.Controls.Label lbFilesOutstanding;
        private System.Windows.Controls.Label lbFilesProcessed;
        private System.Windows.Controls.Label lbNumThreadsRunning;
        private System.Windows.Controls.Label lbDuration;
        private WorkerStatusViewModel ReportViewModel;

        public ThreadScreenController(IThreadScreen screen,
            System.Windows.Controls.Button btnAbort1,
            System.Windows.Controls.Button btnClose1,
            System.Windows.Controls.Button btnPause,
            System.Windows.Controls.Button btnResume,
            System.Windows.Controls.Label lbState,
            System.Windows.Controls.Label lbRemaining,
            System.Windows.Controls.Label lbFilesOutstanding,
            System.Windows.Controls.Label lbFilesProcessed,
            System.Windows.Controls.Label lbNumThreadsRunning,
            System.Windows.Controls.Label lbDuration,
            WorkerStatusViewModel wsvm)
        {
            _mutexMessage = new();
            _screen = screen;
            ((Window)_screen).Closing += OnClosing;
            this.btnAbort1 = btnAbort1; this.btnAbort1.Click += btnAbort1_Click;
            this.btnClose1 = btnClose1; this.btnClose1.Click += btnClose1_Click;
            this.btnPause = btnPause; this.btnPause.Click += btnPause_Click;
            this.btnResume = btnResume; this.btnResume.Click += btnResume_Click;
            this.lbState = lbState;
            this.lbRemaining = lbRemaining;
            this.lbFilesOutstanding = lbFilesOutstanding;
            this.lbFilesProcessed = lbFilesProcessed;
            this.lbNumThreadsRunning = lbNumThreadsRunning;
            this.lbDuration = lbDuration;
            this.ReportViewModel = wsvm;
            _mutexMessage = new();
            _screen = screen;
            _timer = new()
            {
                Interval = new TimeSpan(0, 0, 1)
            };
            _timer.Tick += Refresh;
            _timer.IsEnabled = true;
            ActivateButtonsByStatus(StateEnum.Undefined);
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            //Try to end it in case it's still running.
            try
            {
                _screen.Abort();
            }
            catch { }
        }

        public void Refresh(object sender, EventArgs e)
        {
            try
            {
                ManagerStatus status = _screen.Refresh(sender, e);
                lbRemaining.Content = "";
                ActivateButtonsByStatus(status.state);
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
                _screen.CloseWindow();
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

        private void ActivateButtonsByStatus(StateEnum state)
        {
            switch (state)
            {
                case StateEnum.Running:
                    btnAbort1.IsEnabled = true;
                    btnClose1.IsEnabled = true;
                    btnPause.IsEnabled = true;
                    btnResume.IsEnabled = false;
                    break;
                case StateEnum.Paused:
                    btnAbort1.IsEnabled = true;
                    btnClose1.IsEnabled = true;
                    btnPause.IsEnabled = false;
                    btnResume.IsEnabled = true;
                    break;
                case StateEnum.Stopping:
                    btnAbort1.IsEnabled = false;
                    btnClose1.IsEnabled = true;
                    btnPause.IsEnabled = false;
                    btnResume.IsEnabled = false;
                    break;
                case StateEnum.Stopped:
                    btnAbort1.IsEnabled = false;
                    btnClose1.IsEnabled = true;
                    btnPause.IsEnabled = false;
                    btnResume.IsEnabled = false;
                    _timer.Stop();
                    break;
                case StateEnum.Undefined:
                    btnAbort1.IsEnabled = false;
                    btnClose1.IsEnabled = true;
                    btnPause.IsEnabled = false;
                    btnResume.IsEnabled = false;
                    break;
                default: throw new InvalidOperationException("Undefined StateEnum");
            }
        }

        private void btnClose1_Click(object sender, RoutedEventArgs e)
        {
            try
            { _screen.Abort(); }
            catch { }
            _screen.CloseWindow();
            }

        private void btnAbort1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _screen.Abort();
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
                _screen.Pause();
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
                _screen.Resume();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Resume", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
