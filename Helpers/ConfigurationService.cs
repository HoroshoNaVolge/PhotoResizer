using PhotoPreparation.ViewModels;
using System.IO;
using System.Text.Json;

namespace PhotoPreparation.Helpers
{
    public class ConfigurationService()
    {
        private static readonly string filePath = "settings.json";

        private static SettingsViewModel? settingsViewModel;

        public static void SaveConfiguration()
        {
            string json = JsonSerializer.Serialize(settingsViewModel);
            File.WriteAllText(filePath, json);
        }

        public static SettingsViewModel LoadSettingsConfiguration()
        {
            if (settingsViewModel != null)
                return settingsViewModel;

            if (!File.Exists(filePath))
                return SettingsViewModel.GetDefaultSettingsViewModel();

            string json = File.ReadAllText(filePath);
            return settingsViewModel = JsonSerializer.Deserialize<SettingsViewModel>(json) ?? SettingsViewModel.GetDefaultSettingsViewModel();
        }
    }
}