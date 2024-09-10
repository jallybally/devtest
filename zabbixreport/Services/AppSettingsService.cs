using System.Threading.Tasks;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace zabbixreport.Services
{
    public class AppSettingsService
    {
        private readonly IMongoCollection<AppSettings> _settingsCollection;

        public AppSettings Settings { get; private set; }

        public AppSettingsService(IOptions<MongoDBSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _settingsCollection = database.GetCollection<AppSettings>("Settings");

            LoadSettings().Wait(); // Загрузка настроек при инициализации
        }

        private async Task LoadSettings()
        {
            Settings = await _settingsCollection.Find(_ => true).FirstOrDefaultAsync();
            if (Settings == null)
            {
                Settings = new AppSettings();
                await SaveSettings(); // Сохранение настроек по умолчанию, если они отсутствуют
            }
        }

        public async Task UpdateSettings(AppSettings newSettings)
        {
            Settings.ZabbixServerUrl = newSettings.ZabbixServerUrl;
            Settings.ZabbixApiToken = newSettings.ZabbixApiToken;
            Settings.TemplateGroupId = newSettings.TemplateGroupId;
            Settings.SelectedItems = newSettings.SelectedItems;
            Settings.ServiceTypes = newSettings.ServiceTypes;

            await SaveSettings();
        }

        private async Task SaveSettings()
        {
            var existingSettings = await _settingsCollection.Find(_ => true).FirstOrDefaultAsync();
            if (existingSettings == null)
            {
                await _settingsCollection.InsertOneAsync(Settings);
            }
            else
            {
                await _settingsCollection.ReplaceOneAsync(x => x.Id == existingSettings.Id, Settings);
            }
        }
    }
}
