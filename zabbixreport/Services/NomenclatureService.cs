using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using zabbixreport.DBStruct;

namespace zabbixreport.Services;

public class NomenclatureService
{
    private readonly IMongoCollection<Nomenclature> _nomenclatureCollection;

    public NomenclatureService(IOptions<MongoDBSettings> mongoSettings)
    {
        var mongoClient = new MongoClient(mongoSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(mongoSettings.Value.DatabaseName);

        _nomenclatureCollection = mongoDatabase.GetCollection<Nomenclature>("Nomenclature");
    }

    public async Task<List<Nomenclature>> GetAllAsync() =>
        await _nomenclatureCollection.Find(_ => true).ToListAsync();

    public async Task CreateAsync(Nomenclature nomenclature) =>
        await _nomenclatureCollection.InsertOneAsync(nomenclature);
}
