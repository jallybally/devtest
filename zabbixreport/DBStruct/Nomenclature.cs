using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace zabbixreport.DBStruct;

public class Work
{
    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("hours")]
    public decimal Hours { get; set; }
    
    [BsonElement("quantity")]
    public int Quantity { get; set; }
}

public class Nomenclature
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("hourlyrate")]
    public decimal HourlyRate { get; set; }

    [BsonElement("works")]
    public List<Work> Works { get; set; }

    [BsonElement("servicetype")]
    public string ServiceType { get; set; }
}
