using System;
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

        public ThreadScreenController(IThreadScreen screen, 
            System.Windows.Controls.Button btnAbort1,
            System.Windows.Controls.Button btnClose1,
            System.Windows.Controls.Button btnPause,
            System.Windows.Controls.Button btnResume,
            System.Windows.Controls.Label lbState)
        {
            _mutexMessage = new();
            _screen = screen;
            this.btnAbort1 = btnAbort1; this.btnAbort1.Click += btnAbort1_Click;
            this.btnClose1 = btnClose1; this.btnClose1.Click += btnClose1_Click;
            this.btnPause = btnPause; this.btnPause.Click += btnPause_Click;
            this.btnResume = btnResume; this.btnResume.Click += btnResume_Click;
            this.lbState = lbState;
            _mutexMessage = new();
            _screen = screen;
            _timer = new()
            {
                Interval = new TimeSpan(0, 0, 1)
            };
            _timer.Tick += Refresh;
            _timer.IsEnabled = true;
        }

        public void Refresh(object sender, EventArgs e)
        {
            try
            {
                StateEnum state = _screen.Refresh(sender, e);
                switch (state)
                {
                    case StateEnum.Aborting:
                        btnAbort1.IsEnabled = false;
                        btnClose1.IsEnabled = false;
                        btnPause.IsEnabled = false;
                        btnResume.IsEnabled = false;
                        break;
                    case StateEnum.Running:
                        btnAbort1.IsEnabled = true;
                        btnClose1.IsEnabled = false;
                        btnPause.IsEnabled = true;
                        btnResume.IsEnabled = false;
                        break;
                    case StateEnum.Stopped:
                        btnAbort1.IsEnabled = false;
                        btnClose1.IsEnabled = true;
                        btnPause.IsEnabled = false;
                        btnResume.IsEnabled = false;
                        _timer.Stop();
                        break;
                    case StateEnum.Suspended:
                        btnAbort1.IsEnabled = true;
                        btnClose1.IsEnabled = false;
                        btnPause.IsEnabled = false;
                        btnResume.IsEnabled = true;
                        break;
                    default:
                        btnAbort1.IsEnabled = false;
                        btnClose1.IsEnabled = false;
                        btnPause.IsEnabled = false;
                        btnResume.IsEnabled = false;
                        break;
                }
                lbState.Content = state.ToString();
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

        private void btnClose1_Click(object sender, RoutedEventArgs e)
        {
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
