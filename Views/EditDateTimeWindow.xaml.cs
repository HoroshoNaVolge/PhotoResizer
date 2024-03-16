using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PhotosPreparation
{
    public partial class EditDateTimeWindow : Window
    {
        private string selectedImagePath;

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
                MessageBox.Show($"Error loading image: {ex.Message}");
            }
        }
    }
}