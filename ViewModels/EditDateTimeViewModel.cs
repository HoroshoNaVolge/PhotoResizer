using ExifLib;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.ComponentModel;
using System.Security.RightsManagement;
using System.Windows.Input;

namespace PhotoPreparation.ViewModels
{
    public class EditDateTimeViewModel
    {
        private readonly string filePath;

        public DateTime CurrentDateTime { get; init; }
        public string CurrentDateTimeFormatted => CurrentDateTime.ToString("dd.MM.yyyy HH:mm");
        public DateTime NewDateTime { get; set; }
        public TimeSpan NewTime { get; set; }

        public ICommand SaveCommand { get; }

        public EditDateTimeViewModel(DateTime currentDateTime, string filePath)
        {
            CurrentDateTime = currentDateTime;

            // Чтобы изначально была текущая дата и время в окне редактирования
            NewDateTime = currentDateTime.Date;
            NewTime = new TimeSpan(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);

            this.filePath = filePath;
            SaveCommand = new RelayCommand(Save);
        }

        private void Save()
        {
            NewDateTime = NewDateTime.Date.Add(NewTime);

            using ExifReader reader = new(filePath);
            reader.GetTagValue(ExifTags.DateTimeOriginal, out DateTime dateTime);

        }
    }
}