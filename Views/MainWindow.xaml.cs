using System.Windows;
using PhotoPreparation.ViewModels;
using PhotoPreparation.Views;
using Application = System.Windows.Application;

namespace PhotoPreparation
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            Closed += MainWindow_Closed;

            try
            {
                InitializeComponent();
                DataContext = ((App)Application.Current).MainViewModel;
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