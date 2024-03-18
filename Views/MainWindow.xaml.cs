using System.Windows;
using PhotoPreparation.ViewModels;

namespace PhotosPreparation
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}