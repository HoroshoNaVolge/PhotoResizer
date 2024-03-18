using PhotoPreparation.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PhotoPreparation.Views
{
    /// <summary>
    /// Логика взаимодействия для SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        public SettingsView(int defaultFontSize)
        {
            InitializeComponent();
            foreach (ComboBoxItem item in FontSizeComboBox.Items)
            {
                if (item.Content.ToString() == defaultFontSize.ToString())
                {
                    // Устанавливаем этот элемент в качестве выбранного элемента по умолчанию
                    FontSizeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontSizeComboBox.SelectedItem != null)
            {
                if (int.TryParse((FontSizeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(), out int selectedFontSize))
                {
                    (DataContext as SettingsViewModel)?.SetSelectedFontSize(selectedFontSize);
                }
            }
        }
    }
}
