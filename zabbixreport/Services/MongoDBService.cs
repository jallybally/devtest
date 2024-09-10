using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace zabbixreport.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<AppSettings> _settingsCollection;

        public MongoDBService(IOptions<MongoDBSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _settingsCollection = database.GetCollection<AppSettings>("Settings");
        }

        public async Task<AppSettings> GetSettingsAsync()
        {
            return await _settingsCollection.Find(_ => true).FirstOrDefaultAsync();
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            var existingSettings = await GetSettingsAsync();
            if (existingSettings == null)
            {
                await _settingsCollection.InsertOneAsync(settings);
            }
            else
            {
                settings.Id = existingSettings.Id;
                await _settingsCollection.ReplaceOneAsync(x => x.Id == existingSettings.Id, settings);
            }
        }
        public async Task<List<string>> GetServiceTypesAsync()
        {
            var settings = await GetSettingsAsync();
            return settings?.ServiceTypes ?? new List<string>();
        }
        /*
        public async Task<List<string>> GetWorksAsync()
        {
            var settings = await GetSettingsAsync();
            return settings?.Works ?? new List<string>();
        }
        */
        public async Task<List<string>> GetWorksAsync()
        {
            var settings = await _settingsCollection.Find(_ => true).FirstOrDefaultAsync();
            return settings?.Works ?? new List<string>();
        }
        public async Task SaveServiceTypesAsync(List<string> serviceTypes)
        {
            var settings = await GetSettingsAsync();
            if (settings == null)
            {
                settings = new AppSettings { ServiceTypes = serviceTypes };
            }
            else
            {
                settings.ServiceTypes = serviceTypes;
            }
            await SaveSettingsAsync(settings);
        }
        
    }

    public class MongoDBSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
}
