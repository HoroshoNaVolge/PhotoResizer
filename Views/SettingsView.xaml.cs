using PhotoPreparation.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace PhotoPreparation.Views
{
    /// <summary>
    /// Логика взаимодействия для SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        private bool isOpen = false;
        public SettingsView(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();
            DataContext = settingsViewModel;
            Closing += SettingsView_Closing;
        }
        private void SettingsView_Closing(object sender, CancelEventArgs e)
        {
            // Отменяем закрытие окна и скрываем его вместо этого
            e.Cancel = true;
            Hide();
        }
    }
}
