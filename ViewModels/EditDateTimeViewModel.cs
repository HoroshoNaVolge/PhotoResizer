using ExifLib;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;
using System.Security.RightsManagement;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace PhotoPreparation.ViewModels
{
    public class EditDateTimeViewModel
    {
        private readonly string filePath;

        public delegate void SaveCompletedEventHandler(object sender, EventArgs e);
        public event SaveCompletedEventHandler SaveCompleted;

        public DateTime CurrentDateTime { get; init; }
        public string CurrentDateTimeFormatted => CurrentDateTime.ToString("dd-MM-yyyy HH:mm");
        public DateTime NewDateTime { get; set; }

        public ICommand SaveCommand { get; }

        public EditDateTimeViewModel(DateTime currentDateTime, string filePath)
        {
            CurrentDateTime = currentDateTime;

            //Чтобы изначально была текущая дата и время в окне редактирования

            NewDateTime = currentDateTime == DateTime.MinValue ? DateTime.Now : currentDateTime;
            this.filePath = filePath;
            SaveCommand = new GalaSoft.MvvmLight.CommandWpf.RelayCommand(Save);
        }

        public void Save()
        {
            OnSaveCompleted(EventArgs.Empty);

            using var image = Image.FromFile(filePath);
            // Создаем новый объект метаданных для даты и времени съемки
            var newItem = image.PropertyItems[0];
            newItem.Id = 0x9003; // 0x9003 - код метаданных даты и времени съемки
            newItem.Type = 2; // Тип данных (ASCII строка)
            newItem.Len = 20; // Длина ASCII строки (год, месяц, день, час, минута, секунда)
            newItem.Value = Encoding.ASCII.GetBytes(NewDateTime.ToString("yyyy:MM:dd HH:mm:ss") + '\0');

            // Устанавливаем свойство метаданных изображения
            image.SetPropertyItem(newItem);

            // Сохраняем изображение с обновленными метаданными
            image.Save(filePath + ".DateTimeAdded.jpg", ImageFormat.Jpeg);
        }

        protected virtual void OnSaveCompleted(EventArgs e)
        {
            SaveCompleted.Invoke(this, e);
        }
    }
}