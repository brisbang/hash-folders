using System;
using HashLib7;
using System.Windows;

namespace HashFolders
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Window
    {
        public static IServiceProvider Services { get; private set; }

        public Main()
        {
            InitializeComponent();
            try
            {
                SetConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Initialise", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }


        private static void SetConfig()
        {
            string dataPath = System.Configuration.ConfigurationManager.AppSettings["dataPath"];
            string connStr = System.Configuration.ConfigurationManager.AppSettings["connectionString"];
            string debug = System.Configuration.ConfigurationManager.AppSettings["logDebug"];
            string defaultDrive = System.Configuration.ConfigurationManager.AppSettings["defaultDrive"];
            Config.SetParameters(App.ServiceProvider, dataPath, connStr, defaultDrive, debug == "true");
        }

        private void mnuReportFolders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReportFolder r = new();
                r.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Report folders", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mnuViewFolders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewFolders f = new();
                f.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "View folders", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mnuHashFolders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IndexFolder i = new();
                i.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Index folders", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mnuCompactDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                throw new InvalidOperationException("Compact database is not written");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Compact database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mnuCompareFolders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                throw new InvalidOperationException("Compare folders is not written");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Compare folders", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mnuExitItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Exit", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
    }
}
