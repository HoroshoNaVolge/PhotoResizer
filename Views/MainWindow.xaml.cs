using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using ExifLib;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Diagnostics.Metrics;
using System.Reflection;
using PhotoPreparation.ViewModels;
using System.Diagnostics.CodeAnalysis;

namespace PhotosPreparation
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SelectExifer_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = "Выберите файл для замены метаданных",
                Filter = "Image Files|*.jpg;*.jpeg;*.png"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                statusText.Text = "Выполняется замена метаданных";
                try
                {
                    ReplaceExifData(filePath);
                    statusText.Text = "Замена метаданных завершена";
                }
                catch (ExifLibException ex)
                {
                    statusText.Text = ex.Message;
                }
            }
        }

        private static void ReplaceExifData(string filePath)
        {
            string[] allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            string extension = Path.GetExtension(filePath).ToLower();

            if (!Array.Exists(allowedExtensions, e => e == extension))
            {
                return;
            }

            try
            {
                using (ExifReader reader = new(filePath))
                {
                    if (reader.GetTagValue<DateTime>(ExifTags.DateTimeOriginal, out DateTime dateTime))
                    {
                        EditDateTimeWindow editWindow = new(filePath);
                        EditDateTimeViewModel viewModel = new EditDateTimeViewModel(dateTime, filePath);
                        editWindow.DataContext = viewModel;
                        editWindow.ShowDialog();
                    }
                    else
                    {
                        // В случае, если не удалось найти EXIF-данные, выбрасываем исключение
                        throw new ExifLibException("Unable to locate EXIF content");
                    }
                }
            }
            catch (ExifLibException ex)
            {
                // Перехватываем исключение и передаем его через Task
                throw new ExifLibException($"Ошибка замены метаданных файла: {filePath} \n{ex.Message}", ex);
            }
        }

        private async void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = "Выберите папку с изображениями (открыть любое фото в папке)",
                Filter = "Image Files|*.jpg;*.jpeg;*.png"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string inputFolderPath = Path.GetDirectoryName(openFileDialog.FileName)!;
                string outputFolderName = "Обработанные фото";
                string outputFolderPath = Path.Combine(inputFolderPath, outputFolderName);

                if (!Directory.Exists(outputFolderPath))
                    Directory.CreateDirectory(outputFolderPath);

                int maxWidth = 640;
                int maxHeight = 480;

                statusText.Text = "Выполняется обработка изображений";
                await ProcessImagesAsync(inputFolderPath, outputFolderPath, maxWidth, maxHeight);

                statusText.Text = $"Обработка папки завершена: \n{inputFolderPath} ";
                Process.Start("explorer.exe", outputFolderPath);
            }
        }

        private async Task ProcessImagesAsync(string inputFolderPath, string outputFolderPath, int maxWidth, int maxHeight)
        {
            await Task.Run(() =>
            {
                string[] allowedExtensions = [".jpg", ".jpeg", ".png"];

                if (!Directory.Exists(outputFolderPath))
                    Directory.CreateDirectory(outputFolderPath);

                // Получение списка уже обработанных файлов в выходной папке
                HashSet<string> processedFiles = new(Directory.GetFiles(outputFolderPath));

                string[] files = Directory.GetFiles(inputFolderPath, "*.*", SearchOption.AllDirectories)
                                            .Except(processedFiles)
                                            .ToArray();

                int counter = 1;

                foreach (string filePath in files)
                {
                    string extension = Path.GetExtension(filePath).ToLower();

                    if (Array.Exists(allowedExtensions, e => e == extension))
                    {
                        using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read);
                        try
                        {
                            using ExifReader reader = new(filePath);
                            if (reader.GetTagValue<DateTime>(ExifTags.DateTimeOriginal, out DateTime dateTime))
                            {
                                using Image originalImage = Image.FromFile(filePath);
                                int newWidth = Math.Min(originalImage.Width, maxWidth);
                                int newHeight = Math.Min(originalImage.Height, maxHeight);

                                using Image resizedImage = ResizeImage(originalImage, newWidth, newHeight);
                                using Graphics g = Graphics.FromImage(resizedImage);
                                Font font = new("Arial", 14, System.Drawing.FontStyle.Bold);
                                string watermark = dateTime.ToString("yyyy-MM-dd HH:mm:ss");

                                // Вычисляем размер текста
                                SizeF textSize = g.MeasureString(watermark, font);

                                // Координаты для отрисовки текста в правом нижнем углу
                                int x = resizedImage.Width - (int)textSize.Width - 10;
                                int y = resizedImage.Height - (int)textSize.Height - 10;

                                System.Drawing.Point location = new(x, y);

                                g.DrawImage(resizedImage, 0, 0);
                                g.DrawString(watermark, font, Brushes.White, location);

                                foreach (PropertyItem propertyItem in originalImage.PropertyItems)
                                    resizedImage.SetPropertyItem(propertyItem);

                                string outputFileName = $"Фото {counter}.jpg";
                                counter++;

                                string outputFilePath = Path.Combine(outputFolderPath, outputFileName);
                                resizedImage.Save(outputFilePath, ImageFormat.Jpeg);

                                // Добавление обработанного файла в список
                                processedFiles.Add(filePath);
                            }
                        }
                        catch (ExifLibException ex)
                        {
                            
                            continue;
                        }
                    }
                }
            });
        }

        private static Bitmap ResizeImage(Image image, int maxWidth, int maxHeight)
        {
            double ratioX = (double)maxWidth / image.Width;
            double ratioY = (double)maxHeight / image.Height;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(image.Width * ratio);
            int newHeight = (int)(image.Height * ratio);

            Bitmap newImage = new(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(newImage))
                g.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }
    }
}