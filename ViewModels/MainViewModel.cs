using ExifLib;
using GalaSoft.MvvmLight.CommandWpf;
using Serilog;

using PhotosPreparation;
using PhotoPreparation.Helpers;

using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Input;

using System.ComponentModel;

using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using PhotoPreparation.Views;
using File = System.IO.File;
using System.Text.Json.Serialization;

namespace PhotoPreparation.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly SettingsViewModel settingsViewModel;
        private readonly SettingsView settingsView;

        private static readonly object lockObject = new();

        private string? statusText = MessageConstants.Welcome;

        public MainViewModel(SettingsViewModel settingsViewModel, SettingsView settingsView)
        {
            this.settingsViewModel = settingsViewModel;
            this.settingsView = settingsView;

            SelectImageCommand = new RelayCommand(SelectImage);
            SelectExiferCommand = new RelayCommand(SelectExifer);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
        }


        [JsonIgnore]
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
        [JsonIgnore]
        public ICommand SelectImageCommand { get; }
        [JsonIgnore]
        public ICommand SelectExiferCommand { get; }
        [JsonIgnore]
        public ICommand OpenSettingsCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OpenSettings() => settingsView.ShowDialog();

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
                string outputFolderPath = Path.Combine(inputFolderPath, MessageConstants.OutputFolderName);

                var imageResolution = ImageResizer.GetResolution(settingsViewModel.SelectedResolutionIndex);

                int maxWidth = imageResolution.Item1;
                int maxHeight = imageResolution.Item2;

                StatusText = MessageConstants.ProcessingImagesStatus;

                Stopwatch stopwatch = Stopwatch.StartNew();
                await ProcessImagesAsync(inputFolderPath, maxWidth, maxHeight);

                var processedFilesFailedCount = Directory.GetFiles(inputFolderPath).Where(filePath => filePath.Contains(MessageConstants.NoMetaData)).Count();

                stopwatch.Stop();

                StatusText = processedFilesFailedCount > 0
                ? $"Обработка папки {inputFolderPath} завершена за {(double)stopwatch.ElapsedMilliseconds / 1000} секунд\n{processedFilesFailedCount} ФОТО БЕЗ МЕТАДАННЫХ.\n"
                : $"Обработка папки {inputFolderPath} завершена за {(double)stopwatch.ElapsedMilliseconds / 1000} секунд";

                if (settingsViewModel.OpenFolderAfterProcessing)
                {
                    if (settingsViewModel.DeleteOriginalPhotos)
                        Process.Start(MessageConstants.ProcessedFolderExplorer, inputFolderPath);
                    else
                        Process.Start(MessageConstants.ProcessedFolderExplorer, Path.Combine(inputFolderPath, "Обработанные фото"));
                }

                if (settingsViewModel.DeleteOriginalPhotos)
                {
                    foreach (string file in Directory.GetFiles(inputFolderPath))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при удалении файла {file}: {ex.Message}");
                        }
                    }

                    string[] procFiles = Directory.GetFiles(Path.Combine(inputFolderPath, "ProcessedTemp"));

                    foreach (var procFile in procFiles)
                    {
                        try
                        {
                            var fileName = Path.GetFileName(procFile);
                            File.Move(procFile, Path.Combine(inputFolderPath, fileName));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при перемещении файла {procFile}: {ex.Message}");
                        }
                    }

                    // Удаление временной папки
                    Directory.Delete(Path.Combine(inputFolderPath, "ProcessedTemp"), true);
                }
            }
        }

        private async Task ProcessImagesAsync(string inputFolderPath, int maxWidth, int maxHeight)
        {

            var selectedFontSize = ImageResizer.GetFontSize(settingsViewModel.SelectedFontSizeIndex);

            var tempOutputFolderPath = Path.Combine(inputFolderPath, "ProcessedTemp");
            var finalOutputFolderPath = inputFolderPath;

            if (settingsViewModel.DeleteOriginalPhotos)
                finalOutputFolderPath = tempOutputFolderPath;
            else
            {
                finalOutputFolderPath = Path.Combine(inputFolderPath, MessageConstants.OutputFolderName);

                if (Directory.Exists(finalOutputFolderPath) && Directory.EnumerateFileSystemEntries(finalOutputFolderPath).Any())
                {
                    var result = MessageBox.Show("Папка Обработанные фото уже существует и не пустая. Перезаписать обработанные файлы?", "Предупреждение", MessageBoxButtons.YesNo);
                    if (result == DialogResult.No)
                    {
                        StatusText = MessageConstants.CancelledByUser;
                        return;
                    }
                    Directory.Delete(finalOutputFolderPath, true);
                }
            }

            string[] allowedExtensions = [".jpg", ".jpeg", ".png"];

            Directory.CreateDirectory(finalOutputFolderPath);

            // Получение списка уже обработанных файлов в выходной папке
            HashSet<string> processedFiles = new(Directory.GetFiles(finalOutputFolderPath));

            string[] files = Directory.GetFiles(inputFolderPath, "*.*", SearchOption.TopDirectoryOnly)
                                    .Where(filePath => allowedExtensions.Contains(Path.GetExtension(filePath)?.ToLower()))
                                    .ToArray();

            var counterSuccess = 0;
            var counterFailure = 0;


            await Task.Run(async () =>
                {

                    Parallel.ForEach(files, filePath =>
                {
                    string fileName = Path.GetFileName(filePath);
                    try
                    {
                        using ExifReader reader = new(filePath);
                        if (reader.GetTagValue(ExifTags.DateTimeOriginal, out DateTime dateTime))
                        {
                            using Image originalImage = Image.FromFile(filePath);
                            {
                                var newWidth = Math.Min(originalImage.Width, maxWidth);
                                var newHeight = Math.Min(originalImage.Height, maxHeight);

                                using Image resizedImage = ImageResizer.ResizeImage(originalImage, newWidth, newHeight);
                                using Graphics g = Graphics.FromImage(resizedImage);
                                var font = new Font("Arial", selectedFontSize, FontStyle.Bold);
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

                                lock (lockObject)
                                {
                                    ++counterSuccess;
                                    resizedImage.Save(Path.Combine(finalOutputFolderPath, fileName), ImageFormat.Jpeg);
                                    resizedImage.Dispose();
                                }

                                lock (lockObject)
                                    processedFiles.Add(filePath);
                            }
                        }

                        else
                        {
                            lock (lockObject)
                            {
                                var filePathWithoutExtension = Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {counterFailure + 1}{MessageConstants.JpgExtension}");
                                if (!File.Exists(filePathWithoutExtension))
                                    File.Copy(filePath, Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {++counterFailure}{MessageConstants.JpgExtension}"));
                                processedFiles.Add(Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {++counterFailure}{MessageConstants.JpgExtension}"));
                            }
                        }

                    }
                    catch (ExifLibException ex)
                    {
                        Log.Error(ex, $"Ошибка работы с EXIF: {filePath} {ex.Message}");


                        lock (lockObject)
                        {
                            var filePathWithoutExtension = Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {counterFailure + 1}{MessageConstants.JpgExtension}");
                            if (!File.Exists(filePathWithoutExtension))
                                File.Copy(filePath, Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {++counterFailure}{MessageConstants.JpgExtension}"));
                            processedFiles.Add(filePath);
                            StatusText = $"Ошибка при работе с EXIF файла {Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {++counterFailure}{MessageConstants.JpgExtension}")}";
                        }
                    }

                    //catch (Exception ex)
                    //{
                    //    Log.Error(ex, $"Неизвестная ошибка: {filePath} {ex.Message}");

                    //    lock (lockObject)
                    //    {
                    //        var filePathWithoutExtension = Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {counterFailure + 1}{MessageConstants.JpgExtension}");
                    //        if (!File.Exists(filePathWithoutExtension))
                    //            File.Copy(filePath, Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {++counterFailure}{MessageConstants.JpgExtension}"));
                    //        processedFiles.Add(filePath);
                    //        StatusText = $"Неизвестная ошибка при работе с файлом {Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {++counterFailure}{MessageConstants.JpgExtension}")}";
                    //    }
                    //}


                });

                });
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

            ReplaceExifData(filePath);
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

        private void ReplaceExifData(string filePath)
        {
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

                EditDateTimeWindow editDateTimeWindow = new(filePath);
                EditDateTimeViewModel editDateTimeViewModel = new(dateTime, filePath);
                editDateTimeWindow.DataContext = editDateTimeViewModel;

                editDateTimeViewModel.SaveCompleted += (sender, e) => editDateTimeWindow.Close();

                editDateTimeWindow.ShowDialog();
            }
            catch (ExifLibException ex)
            {
                Log.Error(ex, $"Отсутствуют метаданные: {filePath} {ex.Message}");
                EditDateTimeWindow editWindow = new(filePath);
                EditDateTimeViewModel viewModel = new(DateTime.MinValue, filePath);
                editWindow.DataContext = viewModel;
                viewModel.SaveCompleted += (sender, e) => editWindow.Close();
                editWindow.DataContext = viewModel;
                editWindow.ShowDialog();
            }
        }

        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
