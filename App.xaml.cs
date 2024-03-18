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
            // Получение пути к каталогу для записи логов
            string logDirectory = @"logs\log.txt";


            // Настройка Serilog для записи логов в файл
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logDirectory, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}