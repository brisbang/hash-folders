using System;
using System.Windows;
using HashFolders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

static class Program
{
    public static ServiceProvider serviceProvider;

    [STAThread]
    static void Main()
    {
        var services = new ServiceCollection();

        services.AddLogging(config =>
        {
            config.AddDebug();
            config.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddTransient<HashFolders.Main>();
        serviceProvider = services.BuildServiceProvider();

        var app = new App(); // App.xaml.cs
        app.InitializeComponent(); // Loads resources, etc.
        var mainWindow = serviceProvider.GetRequiredService<HashFolders.Main>();
        app.Run(mainWindow); // Manually launch your window

    }
}
