using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using zabbixreport.Services;

namespace zabbixreport.Pages
{
    public class TemplatesModel : PageModel
    {
        private readonly ZabbixApiClient _zabbixApiClient;
        private readonly AppSettingsService _appSettingsService;

        public TemplatesModel(ZabbixApiClient zabbixApiClient, AppSettingsService appSettingsService)
        {
            _zabbixApiClient = zabbixApiClient;
            _appSettingsService = appSettingsService;
        }

        public List<Template> Templates { get; set; }
        public List<HostGroup> HostGroups { get; set; }
        public Dictionary<string, List<Trigger>> TemplateTriggers { get; set; }
        public Dictionary<string, Dictionary<string, string>> TemplateItems { get; set; }

        public async Task OnGetAsync()
        {
            var settings = _appSettingsService.Settings;

            Templates = await _zabbixApiClient.GetTemplatesInGroupAsync(settings.TemplateGroupId);
            TemplateItems = new Dictionary<string, Dictionary<string, string>>();
            TemplateTriggers = new Dictionary<string, List<Trigger>>();

            foreach (var template in Templates)
            {
                var triggers = await _zabbixApiClient.GetTriggersByTemplateIdAsync(template.templateid);
                TemplateTriggers[template.templateid] = triggers;

                var items = await _zabbixApiClient.GetItemsByTemplateIdAsync(template.templateid);
                TemplateItems[template.templateid] = items;
            }

            var allHostGroups = await _zabbixApiClient.GetAllHostGroupsAsync();
            var excludedGroups = new HashSet<string>
            {
                "Applications",
                "Databases",
                "Discovered hosts",
                "Hypervisors",
                "Linux servers",
                "Virtual machines"
            };

            HostGroups = allHostGroups.Where(g => !excludedGroups.Contains(g.name)).ToList();

        }

        public string ReplaceItemIdsWithNames(string expression, Dictionary<string, string> items)
        {
            string pattern = @"\{(\d+)\}";

            return Regex.Replace(expression, pattern, match =>
            {
                var itemId = match.Groups[1].Value;
                return items.ContainsKey(itemId) ? items[itemId] : match.Value;
            });
        }
    }
}
