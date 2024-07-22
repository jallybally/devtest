using System.Collections.Generic;
using System.Text.Json;

using Newtonsoft.Json;

namespace zabbixreport.Services
{
    public class AppSettingsService
    {
        private const string ConfigFilePath = "appconf.json";

        private readonly ZabbixApiClient _zabbixApiClient;

        public AppSettings Settings { get; private set; }

        public AppSettingsService()
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                Settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
            }
            else
            {
                Settings = new AppSettings();
                SaveSettings();
            }
        }

        public void UpdateSettings(AppSettings newSettings)
        {
            Settings.ZabbixServerUrl = newSettings.ZabbixServerUrl;
            Settings.ZabbixApiToken = newSettings.ZabbixApiToken;
            Settings.TemplateGroupId = newSettings.TemplateGroupId;
            Settings.SelectedItems = newSettings.SelectedItems;
            //Settings.ThresholdValues = newSettings.ThresholdValues;
            SaveSettings();
        }

        private void SaveSettings()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
    }
}
