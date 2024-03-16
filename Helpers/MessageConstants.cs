using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPreparation.Helpers
{
    public static class MessageConstants
    {
        public const string OutputFolderName = "Обработанные фото";

        public const string ImageFilesFilter = "Image Files|*.jpg;*.jpeg;*.png";

        public const string SelectImageTitle = "Выберите папку с изображениями (открыть любое фото в папке)";

        public const string SelectExiferTitle = "Выберите файл для замены метаданных";

        public const string ProcessingImagesStatus = "Выполняется обработка изображений";

        public const string ProcessingFolderStatus = "Обработка папки завершена:";

        public const string ProcessedFolderExplorer = "explorer.exe";

        public const string ProcessedFolderExplorerArguments = "Обработанные фото";

        public const string ProcessingExifStatus = "Выполняется обработка метаданных";

        public const string ProcessedExifStatusSuccess = "Обработка метаданных завершена успешно";

        public const string ExifDateTimeFormat = "yyyy:MM:dd HH:mm:ss";

        public const string CancelledByUser = "Выбор отменён пользователем";

        public const string BadExtension = "Недопустимый формат файла";
    }
}
