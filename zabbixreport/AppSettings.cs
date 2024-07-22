namespace zabbixreport
{
    public class AppSettings
    {
        public string ZabbixServerUrl { get; set; } = "http://default-zabbix-server.com";
        public string ZabbixApiToken { get; set; } = "default-token";
        public int TemplateGroupId { get; set; } = 1;
        public List<string> SelectedItems { get; set; }

        public AppSettings()
        {
            SelectedItems = new List<string>();
        }
    }

}
