using PhotoPreparation.Helpers;
using PhotoPreparation.ViewModels;
using PhotoPreparation.Views;
using Serilog;
using System.IO;
using System.Windows;

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
            ConfigureViewsAndModels(out Window mainWindow);
            Run(mainWindow);
        }

        static new void Run(Window window) => window.Show();
        private static void ConfigureViewsAndModels(out Window mainWindow)
        {

            var configurationService = new ConfigurationService(new SettingsViewModel());

            var settingsView = new SettingsView(configurationService.SettingsViewModel);

            var mainViewModel = new MainViewModel(configurationService.SettingsViewModel, settingsView);

            mainWindow = new MainWindow(mainViewModel);
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