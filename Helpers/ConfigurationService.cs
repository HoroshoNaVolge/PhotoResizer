using PhotoPreparation.ViewModels;
using System.IO;
using System.Text.Json;

namespace PhotoPreparation.Helpers
{
    public class ConfigurationService(SettingsViewModel settingsViewModel)
    {
        private readonly string filePath = "settings.json";
        private SettingsViewModel settingsViewModel = settingsViewModel;


        public SettingsViewModel SettingsViewModel => settingsViewModel;

        public void SaveConfiguration()
        {
            string json = JsonSerializer.Serialize(settingsViewModel);
            File.WriteAllText(filePath, json);
        }

        public void LoadConfiguration()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                settingsViewModel = JsonSerializer.Deserialize<SettingsViewModel>(json) ?? throw new NullReferenceException(nameof(SettingsViewModel));
            }
            else
                settingsViewModel = new SettingsViewModel
                {
                    SelectedFontSizeIndex = 14, // Установим начальное значение по умолчанию
                    OpenFolderAfterProcessing = true,
                    SelectedResolutionIndex = 0,
                    DeleteOriginalPhotos = false,
                };
        }
    }
}