﻿using PhotoPreparation.Helpers;
using PhotoPreparation.ViewModels;
using PhotoPreparation.Views;
using Serilog;
using System.IO;
using System.Windows;
using Application = System.Windows.Application;

namespace PhotoPreparation
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public MainViewModel? MainViewModel { get; private set; }

        public App()
        {
            ConfigureLogger();
            ConfigureViewsAndModels();
        }

        private void ConfigureViewsAndModels()
        {

            var configurationService = new ConfigurationService();

            var settingsView = new SettingsView(ConfigurationService.LoadSettingsConfiguration());

            MainViewModel = new MainViewModel(ConfigurationService.LoadSettingsConfiguration(), settingsView, new ImageProcessingService(ConfigurationService.LoadSettingsConfiguration())); // переписать эту ёбань

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