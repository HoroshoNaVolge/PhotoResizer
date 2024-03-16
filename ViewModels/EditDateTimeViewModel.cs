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
        public DateTime NewDate { get; set; }
        public TimeSpan NewTime { get; set; }

        public ICommand SaveCommand { get; }

        public EditDateTimeViewModel(DateTime currentDateTime, string filePath)
        {
            CurrentDateTime = currentDateTime;
            NewDate = currentDateTime.Date;
            NewTime = new TimeSpan(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second);
            SaveCommand = new RelayCommand(Save);
            this.filePath = filePath;
        }

        private void Save()
        {
            NewDate = NewDate.Date.Add(NewTime);

            using ExifReader reader = new(filePath);
            reader.GetTagValue(ExifTags.DateTimeOriginal, out DateTime dateTime);

        }
    }
}