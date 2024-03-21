using PhotoPreparation.Helpers;
using System.ComponentModel;

namespace PhotoPreparation.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private bool deleteOriginalPhotos;
        private bool openFolderAfterProcessing;
        private int selectedFontSizeIndex;
        private int selectedResolutionIndex;

        public bool DeleteOriginalPhotos
        {
            get { return deleteOriginalPhotos; }
            set
            {
                if (deleteOriginalPhotos != value)
                {
                    deleteOriginalPhotos = value;
                    OnPropertyChanged(nameof(DeleteOriginalPhotos));
                    ConfigurationService.SaveConfiguration();
                }
            }
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
                    ConfigurationService.SaveConfiguration();
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
                    ConfigurationService.SaveConfiguration();
                }
            }
        }

        public int SelectedResolutionIndex
        {
            get { return selectedResolutionIndex; }
            set
            {
                if (selectedResolutionIndex != value)
                {
                    selectedResolutionIndex = value;
                    OnPropertyChanged(nameof(SelectedResolutionIndex));
                    ConfigurationService.SaveConfiguration();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public static SettingsViewModel GetDefaultSettingsViewModel() => new()
        {
            SelectedFontSizeIndex = 14,
            OpenFolderAfterProcessing = true,
            SelectedResolutionIndex = 0,
            DeleteOriginalPhotos = false,
        };
    }
}