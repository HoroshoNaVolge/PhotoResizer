using ExifLib;
using PhotoPreparation.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPreparation.Helpers
{
    public class ImageProcessingService(SettingsViewModel settingsViewModel)
    {
        private static readonly object lockObject = new();
        public event Action<string>? StatusTextChanged;
        public event Action<double>? ProgressValueChanged;

        public async Task<bool> ProcessImagesAsync(string inputFolderPath, int maxWidth, int maxHeight)
        {
            var selectedFontSize = GetFontSize(settingsViewModel.SelectedFontSizeIndex);

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
                        OnStatusTextChanged(MessageConstants.CancelledByUser);
                        return false;
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
            StringBuilder stringBuilder = new();

            await Task.Run(() =>
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

                                using Image resizedImage = ImageProcessingService.ResizeImage(originalImage, newWidth, newHeight);
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
                            OnStatusTextChanged($"Ошибка при работе с EXIF файла {Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {++counterFailure}{MessageConstants.JpgExtension}")}");
                        }
                    }

                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Неизвестная ошибка: {filePath} {ex.Message}");

                        lock (lockObject)
                        {
                            var filePathWithoutExtension = Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {counterFailure + 1}{MessageConstants.JpgExtension}");
                            if (!File.Exists(filePathWithoutExtension))
                                File.Copy(filePath, Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {++counterFailure}{MessageConstants.JpgExtension}"));
                            processedFiles.Add(filePath);
                            OnStatusTextChanged($"Неизвестная ошибка при работе с файлом {Path.Combine(finalOutputFolderPath, $"{MessageConstants.NoMetaData} {++counterFailure}{MessageConstants.JpgExtension}")}");
                        }
                    }

                    finally
                    {
                        OnProgressValueChanged(1.0 / files.Length * 100.0);
                        OnStatusTextChanged($"Обработано {counterSuccess} файлов из {files.Length}");
                    }
                });
            });
            return true;

        }
        public static Bitmap ResizeImage(Image image, int maxWidth, int maxHeight)
        {
            var ratio = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);
            using var g = Graphics.FromImage(newImage);
            g.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }

        public static (int, int) GetResolution(int index)
        {
            return index switch
            {
                0 => (640, 480),
                1 => (800, 600),
                2 => (1024, 768),
                3 => (1280, 1024),
                _ => (640, 480),
            };
        }

        public static int GetFontSize(int index)
        {
            return index switch
            {
                0 => 10,
                1 => 12,
                2 => 14,
                3 => 16,
                4 => 18,
                5 => 22,
                _ => 14
            };
        }

        public void OnStatusTextChanged(string text)
        {
            StatusTextChanged?.Invoke(text);
        }
        public void OnProgressValueChanged(double value)
        {
            ProgressValueChanged?.Invoke(value);
        }
    }
}