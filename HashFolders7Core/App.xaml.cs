using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HashFolders
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();

            // Configure logging
            serviceCollection.AddLogging(config =>
            {
                config.AddDebug();
                config.SetMinimumLevel(LogLevel.Information);
            });

            // Register your services and windows
            serviceCollection.AddTransient<Main>();
            //serviceCollection.AddTransient<IMyService, MyService>();

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<Main>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        public void InitializeComponent()
        {

        }
    }
}
