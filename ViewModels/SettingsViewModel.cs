using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPreparation.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private bool openFolderAfterProcessing;
        private int selectedFontSizeIndex;

        public SettingsViewModel()
        {
            SelectedFontSizeIndex = 14; // Установим начальное значение по умолчанию
            OpenFolderAfterProcessing = true;
        }

        public bool OpenFolderAfterProcessing
        {
            get { return openFolderAfterProcessing; }
            set
            {
                if (openFolderAfterProcessing != value)
                {
                    openFolderAfterProcessing = value;
                    OnPropertyChanged(nameof(OpenFolderAfterProcessing));
                }
            }
        }

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void SetSelectedFontSize(int fontSize) => SelectedFontSizeIndex = fontSize;
    }
}

