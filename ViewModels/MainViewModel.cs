using ExifLib;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using PhotosPreparation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using PhotoPreparation.Helpers;
using System.ComponentModel;
using Serilog;

namespace PhotoPreparation.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

        private string? statusText;
        public string? StatusText
        {
            get { return statusText; }
            set
            {
                if (statusText != value)
                {
                    statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public ICommand SelectImageCommand { get; }
        public ICommand SelectExiferCommand { get; }

        public MainViewModel()
        {
            SelectImageCommand = new RelayCommand(SelectImage);
            SelectExiferCommand = new RelayCommand(SelectExifer);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async void SelectImage()
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = MessageConstants.SelectImageTitle,
                Filter = MessageConstants.ImageFilesFilter
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string inputFolderPath = Path.GetDirectoryName(openFileDialog.FileName)!;
                string outputFolderName = MessageConstants.OutputFolderName;
                string outputFolderPath = Path.Combine(inputFolderPath, outputFolderName);

                if (!Directory.Exists(outputFolderPath))
                    Directory.CreateDirectory(outputFolderPath);

                int maxWidth = 640;
                int maxHeight = 480;

                StatusText = MessageConstants.ProcessingImagesStatus;
                await ProcessImagesAsync(inputFolderPath, outputFolderPath, maxWidth, maxHeight);

                //StatusText = $"{MessageConstants.ProcessingFolderStatus}\n{inputFolderPath} ";
                Process.Start(MessageConstants.ProcessedFolderExplorer, outputFolderPath);
            }
        }

        private void SelectExifer()
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = MessageConstants.SelectExiferTitle,
                Filter = MessageConstants.ImageFilesFilter
            };

            if (openFileDialog.ShowDialog() == false)
            {
                StatusText = MessageConstants.CancelledByUser;
                return;
            }

            string filePath = openFileDialog.FileName;

            StatusText = MessageConstants.ProcessingExifStatus;
            try
            {
                ReplaceExifData(filePath);
                StatusText = MessageConstants.ProcessedExifStatusSuccess;
            }
            catch (ExifLibException ex)
            {
                StatusText = ex.Message;
            }
        }

        private void ReplaceExifData(string filePath)
        {
            string[] allowedExtensions = [".jpg", ".jpeg", ".png"];
            string extension = Path.GetExtension(filePath).ToLower();

            if (!Array.Exists(allowedExtensions, e => e == extension))
            {
                StatusText = MessageConstants.BadExtension;
                return;
            }

            try
            {
                using ExifReader reader = new(filePath);

                reader.GetTagValue<DateTime>(ExifTags.DateTimeOriginal, out DateTime dateTime);

                EditDateTimeWindow editWindow = new(filePath);
                EditDateTimeViewModel viewModel = new(dateTime, filePath);
                editWindow.DataContext = viewModel;

                editWindow.ShowDialog();
            }
            catch (ExifLibException ex)
            {
                Log.Error(ex, $"Неизвестная ошибка при обработке EXIF: {filePath} {ex.Message}");
                throw;
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

                string[] files = Directory.GetFiles(inputFolderPath, "*.*", SearchOption.TopDirectoryOnly)
                                            .Except(processedFiles)
                                            .Where(filePath => allowedExtensions.Contains(Path.GetExtension(filePath)?.ToLower()))
                                            .ToArray();

                var counterSuccess = 0;
                var counterFailure = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();

                Parallel.ForEach(files, filePath =>
                {
                    try
                    {
                        using ExifReader reader = new(filePath);
                        if (reader.GetTagValue<DateTime>(ExifTags.DateTimeOriginal, out DateTime dateTime))
                        {
                            using Image originalImage = Image.FromFile(filePath);

                            var newWidth = Math.Min(originalImage.Width, maxWidth);
                            var newHeight = Math.Min(originalImage.Height, maxHeight);

                            using Image resizedImage = ResizeImage(originalImage, newWidth, newHeight);
                            using Graphics g = Graphics.FromImage(resizedImage);
                            var font = new Font("Arial", 14, FontStyle.Bold);
                            var watermark = dateTime.ToString("yyyy-MM-dd HH:mm");

                            SizeF textSize = g.MeasureString(watermark, font);

                            // Координаты для отрисовки текста в правом нижнем углу
                            var x = resizedImage.Width - (int)textSize.Width - 10;
                            var y = resizedImage.Height - (int)textSize.Height - 10;
                            Point location = new(x, y);

                            g.DrawImage(resizedImage, 0, 0);
                            g.DrawString(watermark, font, Brushes.White, location);

                            foreach (PropertyItem propertyItem in originalImage.PropertyItems)
                                resizedImage.SetPropertyItem(propertyItem);

                            resizedImage.Save(Path.Combine(outputFolderPath, $"Фото {++counterSuccess}.jpg"), ImageFormat.Jpeg);

                            processedFiles.Add(filePath);
                        }

                        else
                        {
                            var filePathWithoutExtension = Path.Combine(outputFolderPath, $"НЕТ МЕТАДАННЫХ {counterFailure + 1}.jpg");
                            if (!File.Exists(filePathWithoutExtension))
                                File.Copy(filePath, Path.Combine(outputFolderPath, $"НЕТ МЕТАДАННЫХ {++counterFailure}.jpg"));
                        }

                    }
                    catch (ExifLibException)
                    {
                        var filePathWithoutExtension = Path.Combine(outputFolderPath, $"НЕТ МЕТАДАННЫХ {counterFailure + 1}.jpg");
                        if (!File.Exists(filePathWithoutExtension))
                            File.Copy(filePath, Path.Combine(outputFolderPath, $"НЕТ МЕТАДАННЫХ {++counterFailure}.jpg"));
                    }
                });
                stopwatch.Stop();

                StatusText = processedFiles.Any(filePath => filePath.Contains("НЕТ МЕТАДАННЫХ"))
                    ? $"Обработка папки {inputFolderPath} завершена за {(double)stopwatch.ElapsedMilliseconds / 1000} секунд\n ЕСТЬ ФОТО БЕЗ МЕТАДАННЫХ. Проверьте в обработанной папке"
                    : $"Обработка папки {inputFolderPath} завершена за {(double)stopwatch.ElapsedMilliseconds / 1000} секунд";
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

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
