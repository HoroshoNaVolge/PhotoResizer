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
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

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

            ReplaceExifData(filePath, out bool modified);
            // Удаляем исходный файл и переименовываем обработанный файл

            if (!File.Exists(filePath + "modified"))
            {
                StatusText = MessageConstants.CancelledByUser;
                return;
            }

            File.Delete(filePath);
            File.Move(filePath + "modified", filePath);
            StatusText = MessageConstants.ProcessedExifStatusSuccess;
        }

        private void ReplaceExifData(string filePath, out bool modified)
        {
            modified = false;
            if (filePath == null)
            {
                StatusText = MessageConstants.CancelledByUser;
                return;
            }
            string[] allowedExtensions = [".jpg", ".jpeg", ".png"];
            string extension = Path.GetExtension(filePath).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                StatusText = MessageConstants.BadExtension;
                return;
            }

            // TO DO: Переделать на асинхронный метод (copilot)
            // унифицировать обработку исключений
            try
            {
                using ExifReader reader = new(filePath);

                reader.GetTagValue<DateTime>(ExifTags.DateTimeOriginal, out DateTime dateTime);

                EditDateTimeWindow editDateTimeWindow = new EditDateTimeWindow(filePath);
                EditDateTimeViewModel editDateTimeViewModel = new EditDateTimeViewModel(dateTime, filePath);
                editDateTimeWindow.DataContext = editDateTimeViewModel;

                editDateTimeViewModel.SaveCompleted += (sender, e) => editDateTimeWindow.Close();

                editDateTimeWindow.ShowDialog();

                modified = true;
            }
            catch (ExifLibException ex)
            {
                Log.Error(ex, $"Отсутствуют метаданные: {filePath} {ex.Message}");
                EditDateTimeWindow editWindow = new EditDateTimeWindow(filePath);
                EditDateTimeViewModel viewModel = new EditDateTimeViewModel(DateTime.MinValue, filePath);
                editWindow.DataContext = viewModel;
                viewModel.SaveCompleted += (sender, e) => editWindow.Close();
                editWindow.DataContext = viewModel;
                editWindow.ShowDialog();
            }
        }

        private async Task ProcessImagesAsync(string inputFolderPath, string outputFolderPath, int maxWidth, int maxHeight)
        {
            await Task.Run(() =>
            {
                string[] allowedExtensions = [".jpg", ".jpeg", ".png"];

                if (Directory.Exists(outputFolderPath) && Directory.EnumerateFileSystemEntries(outputFolderPath).Any())
                {
                    var result = MessageBox.Show("Директория уже существует и не пустая. Перезаписать обработанные файлы?", "Предупреждение", MessageBoxButtons.YesNo);
                    if (result == DialogResult.No)
                        return;
                }
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
                            var watermark = dateTime.ToString("dd/MM/yyyy HH:mm");

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
                    catch (ExifLibException ex)
                    {
                        Log.Error(ex, $"Ошибка работы с EXIF: {filePath} {ex.Message}");

                        var filePathWithoutExtension = Path.Combine(outputFolderPath, $"НЕТ МЕТАДАННЫХ {counterFailure + 1}.jpg");
                        if (!File.Exists(filePathWithoutExtension))
                            File.Copy(filePath, Path.Combine(outputFolderPath, $"НЕТ МЕТАДАННЫХ {++counterFailure}.jpg"));
                    }

                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Неизвестная ошибка: {filePath} {ex.Message}");

                        var filePathWithoutExtension = Path.Combine(outputFolderPath, $"НЕТ МЕТАДАННЫХ {counterFailure + 1}.jpg");
                        if (!File.Exists(filePathWithoutExtension))
                            File.Copy(filePath, Path.Combine(outputFolderPath, $"НЕТ МЕТАДАННЫХ {++counterFailure}.jpg"));

                        StatusText = $"Неизвестная ошибка при работе с {filePath}";
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
            var ratio = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);
            using var g = Graphics.FromImage(newImage);
            g.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
