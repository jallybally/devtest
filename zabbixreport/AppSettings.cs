using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace zabbixreport
{
    public class AppSettings
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string ZabbixServerUrl { get; set; } = "http://default-zabbix-server.com";
        public string ZabbixApiToken { get; set; } = "default-token";
        public int TemplateGroupId { get; set; } = 1;
        public List<string> SelectedItems { get; set; }
        public List<string> ServiceTypes { get; set; }
        public List<string> Works {get; set;}

        public AppSettings()
        {
            SelectedItems = new List<string>();
            ServiceTypes = new List<string>();
            Works = new List<string>();
        }
    }

}
