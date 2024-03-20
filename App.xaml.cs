using PhotoPreparation.ViewModels;
using PhotoPreparation.Views;
using Serilog;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace PhotosPreparation
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public App()
        {
            ConfigureLogger();
            ConfigureViewsAndModels();
        }

        private static void ConfigureViewsAndModels()
        {
            var settingsViewModel = new SettingsViewModel();
            var settingsView = new SettingsView(settingsViewModel);

            var mainViewModel = new MainViewModel(settingsViewModel, settingsView);
            var mainWindow = new MainWindow(mainViewModel);

            mainWindow.Show();
        }

        private static void ConfigureLogger()
        {
            string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"PhotoPreparation\logs\log.txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logDirectory, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}