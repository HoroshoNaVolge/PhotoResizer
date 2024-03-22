using Serilog;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Xceed.Wpf.Toolkit;

namespace PhotoPreparation
{
    public partial class EditDateTimeWindow : Window, INotifyPropertyChanged
    {
        private readonly string selectedImagePath;

        private DateTime newDateTime;
        public DateTime NewDateTime
        {
            get { return newDateTime; }
            set
            {
                if (newDateTime != value)
                {
                    newDateTime = value;
                    OnPropertyChanged(nameof(NewDateTime));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public EditDateTimeWindow(string imagePath)
        {
            InitializeComponent();

            // Сохраняем путь к выбранному изображению
            selectedImagePath = imagePath;

            // Загружаем и отображаем изображение в элементе Image
            LoadAndDisplayImage();
        }

        private void LoadAndDisplayImage()
        {
            try
            {
                // Создаем новый BitmapImage
                BitmapImage bitmap = new();

                // Загружаем изображение из файла
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(selectedImagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                // Устанавливаем BitmapImage как источник для элемента Image
                selectedImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при загрузке изображения");
                System.Windows.MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}");
            }
        }
        private void MyDateTimeUpDown_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is DateTimeUpDown dateTimeUpDown)
            {
                // Увеличение или уменьшение времени на 15 минут при каждом шаге колеса мыши
                if (e.Delta > 0)
                {
                    dateTimeUpDown.Value = dateTimeUpDown.Value?.AddMinutes(1);
                }
                else
                {
                    dateTimeUpDown.Value = dateTimeUpDown.Value?.AddMinutes(-1);
                }
            }
        }
    }
}