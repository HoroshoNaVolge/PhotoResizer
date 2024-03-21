using System.Windows;
using PhotoPreparation.ViewModels;
using PhotoPreparation.Views;

namespace PhotosPreparation
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel mainViewModel)
        {
            Closed += MainWindow_Closed;

            try
            {
                InitializeComponent();
                DataContext = mainViewModel;
            }
            catch (Exception ex)
            {
                // Обработка ошибки и вывод сообщения
                System.Windows.MessageBox.Show($"An error occurred in MainWindow constructor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}