using ExifLib;
using GalaSoft.MvvmLight.CommandWpf;
using Serilog;

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
using System.Text;

using Visibility = System.Windows.Visibility;

namespace PhotoPreparation.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly SettingsViewModel settingsViewModel;
        private readonly SettingsView settingsView;
        private readonly ImageProcessingService imageProcessingService;



        private string? statusText = MessageConstants.Welcome;
        private string? statusNoDateTakenProcessed;

        private double progressValue;
        private Visibility progressBarVisibility = Visibility.Hidden;

        public MainViewModel(SettingsViewModel settingsViewModel, SettingsView settingsView, ImageProcessingService imageProcessingService)
        {
            this.settingsViewModel = settingsViewModel;
            this.settingsView = settingsView;
            this.imageProcessingService = imageProcessingService;

            imageProcessingService.StatusTextChanged += OnStatusTextChanged;
            imageProcessingService.ProgressValueChanged += OnProgressValueChanged;

            SelectImageCommand = new RelayCommand(SelectImage);
            SelectExiferCommand = new RelayCommand(SelectExifer);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
        }

        [JsonIgnore]
        public double ProgressValue
        {
            get { return progressValue; }
            set
            {
                if (progressValue != value)
                {
                    progressValue = value;
                    OnPropertyChanged(nameof(ProgressValue));
                    ProgressBarVisibility = progressValue > 0 && progressValue <= 100 ? Visibility.Visible : Visibility.Hidden;
                }
            }
        }

        public Visibility ProgressBarVisibility
        {
            get { return progressBarVisibility; }
            set
            {
                if (progressBarVisibility != value)
                {
                    progressBarVisibility = value;
                    OnPropertyChanged(nameof(ProgressBarVisibility));
                }
            }
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

        public string? StatusNoDateTakenProcessed
        {
            get { return statusNoDateTakenProcessed; }
            set
            {
                if (statusNoDateTakenProcessed != value)
                {
                    statusNoDateTakenProcessed = value;
                    OnPropertyChanged(nameof(StatusNoDateTakenProcessed));
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

                var imageResolution = ImageProcessingService.GetResolution(settingsViewModel.SelectedResolutionIndex);

                int maxWidth = imageResolution.Item1;
                int maxHeight = imageResolution.Item2;

                StatusText = MessageConstants.ProcessingImagesStatus;
                ProgressValue = 0.0;
                StatusNoDateTakenProcessed = null;

                Stopwatch stopwatch = Stopwatch.StartNew();

                bool imageProcsessPerformed = await imageProcessingService.ProcessImagesAsync(inputFolderPath, maxWidth, maxHeight);
                ProgressValue = 100.0;

                if (!imageProcsessPerformed)
                    return;


                var processedFilesFailedCount = 0;
                if (!settingsViewModel.DeleteOriginalPhotos)
                    processedFilesFailedCount = Directory.GetFiles(outputFolderPath).Where(filePath => filePath.Contains(MessageConstants.NoMetaData)).Count();
                else
                    processedFilesFailedCount = Directory.GetFiles(inputFolderPath).Where(filePath => filePath.Contains(MessageConstants.NoMetaData)).Count();

                stopwatch.Stop();


                StatusText = "Обработка папки " + inputFolderPath + " завершена за " + ((double)stopwatch.ElapsedMilliseconds / 1000) + " секунд\n\n";

                if (processedFilesFailedCount > 0)
                    StatusNoDateTakenProcessed = $"\n\n{processedFilesFailedCount} фото без даты съёмки.";

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

        private void OnStatusTextChanged(string statusText) => StatusText = statusText;
        private void OnProgressValueChanged(double progressValue) => ProgressValue += progressValue;
    }
}
