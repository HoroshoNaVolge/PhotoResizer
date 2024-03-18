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

namespace PhotoPreparation.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly SettingsViewModel settingsViewModel = new();


        public bool OpenFolderAfterProcessing = true;

        int selectedFontSizeIndex = 14;
        public int SelectedFontSizeIndex
        {
            get { return selectedFontSizeIndex; }
            set
            {
                if (selectedFontSizeIndex != value)
                {
                    selectedFontSizeIndex = value;
                    OnPropertyChanged(nameof(SelectedFontSizeIndex));
                }
            }
        }

        private string? statusText = MessageConstants.Welcome;
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
        public ICommand OpenSettingsCommand { get; }

        public MainViewModel()
        {
            SelectImageCommand = new RelayCommand(SelectImage);
            SelectExiferCommand = new RelayCommand(SelectExifer);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
        }

        public event PropertyChangedEventHandler? PropertyChanged;


        private void OpenSettings()
        {
            SettingsView settingsView = new()
            {
                DataContext = settingsViewModel
            };

            OpenFolderAfterProcessing = settingsViewModel.OpenFolderAfterProcessing;
            SelectedFontSizeIndex = settingsViewModel.SelectedFontSizeIndex;
            settingsViewModel.PropertyChanged += SettingsViewModel_PropertyChanged;
            settingsView.ShowDialog();
        }

        private void SettingsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "OpenFolderAfterProcessing")
            {
                // Обновить свойство OpenFolderAfterProcessing в MainViewModel
                OpenFolderAfterProcessing = settingsViewModel.OpenFolderAfterProcessing;
            }
            else if (e.PropertyName == "SelectedFontSizeIndex")
            {
                // Обновить свойство SelectedFontSizeIndex в MainViewModel
                SelectedFontSizeIndex = settingsViewModel.SelectedFontSizeIndex;
            }
        }

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

                int maxWidth = 640;
                int maxHeight = 480;

                StatusText = MessageConstants.ProcessingImagesStatus;
                await ProcessImagesAsync(inputFolderPath, maxWidth, maxHeight);


                if (OpenFolderAfterProcessing)
                    Process.Start(MessageConstants.ProcessedFolderExplorer, outputFolderPath);
            }
        }

        private async Task ProcessImagesAsync(string inputFolderPath, int maxWidth, int maxHeight)
        {
            var outputFolderPath = Path.Combine(inputFolderPath, MessageConstants.OutputFolderName);
            if (Directory.Exists(outputFolderPath) && Directory.EnumerateFileSystemEntries(outputFolderPath).Any())
            {
                var result = MessageBox.Show("Директория уже существует и не пустая. Перезаписать обработанные файлы?", "Предупреждение", MessageBoxButtons.YesNo);
                if (result == DialogResult.No)
                    return;
            }

            await Task.Run(() =>
            {
                string[] allowedExtensions = [".jpg", ".jpeg", ".png"];

                Directory.CreateDirectory(outputFolderPath);

                // Получение списка уже обработанных файлов в выходной папке
                HashSet<string> processedFiles = new(Directory.GetFiles(outputFolderPath));

                string[] files = Directory.GetFiles(inputFolderPath, "*.*", SearchOption.TopDirectoryOnly)
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

                            using Image resizedImage = ImageResizer.ResizeImage(originalImage, newWidth, newHeight);
                            using Graphics g = Graphics.FromImage(resizedImage);
                            var font = new Font("Arial", SelectedFontSizeIndex, FontStyle.Bold);
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

                            resizedImage.Save(Path.Combine(outputFolderPath, $"Фото {++counterSuccess}{MessageConstants.JpgExtension}"), ImageFormat.Jpeg);

                            processedFiles.Add(filePath);
                        }

                        else
                        {
                            var filePathWithoutExtension = Path.Combine(outputFolderPath, $"{MessageConstants.NoMetaData} {counterFailure + 1}{MessageConstants.JpgExtension}");
                            if (!File.Exists(filePathWithoutExtension))
                                File.Copy(filePath, Path.Combine(outputFolderPath, $"{MessageConstants.NoMetaData} {++counterFailure}{MessageConstants.JpgExtension}"));
                        }

                    }
                    catch (ExifLibException ex)
                    {
                        Log.Error(ex, $"Ошибка работы с EXIF: {filePath} {ex.Message}");

                        var filePathWithoutExtension = Path.Combine(outputFolderPath, $"{MessageConstants.NoMetaData} {counterFailure + 1}{MessageConstants.JpgExtension}");
                        if (!File.Exists(filePathWithoutExtension))
                            File.Copy(filePath, Path.Combine(outputFolderPath, $"{MessageConstants.NoMetaData} {++counterFailure}{MessageConstants.JpgExtension}"));
                    }

                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Неизвестная ошибка: {filePath} {ex.Message}");

                        var filePathWithoutExtension = Path.Combine(outputFolderPath, $"{MessageConstants.NoMetaData} {counterFailure + 1}{MessageConstants.JpgExtension}");
                        if (!File.Exists(filePathWithoutExtension))
                            File.Copy(filePath, Path.Combine(outputFolderPath, $"{MessageConstants.NoMetaData} {++counterFailure}{MessageConstants.JpgExtension}"));

                        StatusText = $"Неизвестная ошибка при работе с {filePath}";
                    }

                });

                stopwatch.Stop();

                StatusText = processedFiles.Any(filePath => filePath.Contains(MessageConstants.NoMetaData))
                    ? $"Обработка папки {inputFolderPath} завершена за {(double)stopwatch.ElapsedMilliseconds / 1000} секунд\n ЕСТЬ ФОТО БЕЗ МЕТАДАННЫХ. Проверьте в обработанной папке"
                    : $"Обработка папки {inputFolderPath} завершена за {(double)stopwatch.ElapsedMilliseconds / 1000} секунд";
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

                EditDateTimeWindow editDateTimeWindow = new EditDateTimeWindow(filePath);
                EditDateTimeViewModel editDateTimeViewModel = new EditDateTimeViewModel(dateTime, filePath);
                editDateTimeWindow.DataContext = editDateTimeViewModel;

                editDateTimeViewModel.SaveCompleted += (sender, e) => editDateTimeWindow.Close();

                editDateTimeWindow.ShowDialog();
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



        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
