using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using zabbixreport.Services;

namespace zabbixreport.Pages
{
    public class SettingsModel : PageModel
    {
        private readonly AppSettingsService _appSettingsService;
        private readonly ZabbixApiClient _zabbixApiClient;

        public SettingsModel(AppSettingsService appSettingsService, ZabbixApiClient zabbixApiClient)
        {
            _appSettingsService = appSettingsService;
            _zabbixApiClient = zabbixApiClient;
        }
        [BindProperty]
        public List<string> SelectedItems { get; set; }
        [BindProperty]
        public string ZabbixServerUrl { get; set; }
        [BindProperty]
        public string ZabbixApiToken { get; set; }
        [BindProperty]
        public int TemplateGroupId { get; set; }
        [BindProperty]
        public AppSettings Settings { get; set; }
        [BindProperty]
        public Dictionary<string, bool> SelectedItemsState { get; set; }

        public List<Template> Templates { get; set; }
        public List<HostGroup> HostGroups { get; set; }
        public Dictionary<string, List<Trigger>> TemplateTriggers { get; set; }
        public Dictionary<string, Dictionary<string, string>> TemplateItems { get; set; }
        public Dictionary<string, List<ItemPrototype>> TemplatePrototypes { get; set; }

        public async Task OnGetAsync()
        {
            var settings = _appSettingsService.Settings;

            var selectedItems = settings.SelectedItems.ToHashSet();
            SelectedItemsState = new Dictionary<string, bool>();

            ZabbixServerUrl = settings.ZabbixServerUrl;
            ZabbixApiToken = settings.ZabbixApiToken;
            TemplateGroupId = settings.TemplateGroupId;

            Templates = await _zabbixApiClient.GetTemplatesInGroupAsync(settings.TemplateGroupId);
            TemplateItems = new Dictionary<string, Dictionary<string, string>>();
            TemplatePrototypes = new Dictionary<string, List<ItemPrototype>>();

            foreach (var template in Templates)
            {
                var items = await _zabbixApiClient.GetItemsByTemplateIdAsync(template.templateid);
                TemplateItems[template.templateid] = items;
                var itemPrototypes = await _zabbixApiClient.GetItemPrototypesByTemplateIdAsync(template.templateid);
                var prototypes = await _zabbixApiClient.GetItemPrototypesByTemplateIdAsync(template.templateid);
                TemplatePrototypes[template.templateid] = prototypes;

                foreach (var item in items)
                {
                    if (selectedItems.Contains(item.Key))
                    {
                        SelectedItemsState[item.Key] = true;
                    }
                    else
                    {
                        SelectedItemsState[item.Key] = false;
                    }
                }
                foreach (var prototype in prototypes)
                {
                    SelectedItemsState[prototype.itemid] = settings.SelectedItems.Contains(prototype.itemid);
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _appSettingsService.Settings.SelectedItems.Clear();

            foreach (var (itemId, selected) in SelectedItemsState)
            {
                if (selected)
                {
                    _appSettingsService.Settings.SelectedItems.Add(itemId);
                }
            }

            var newSettings = new AppSettings
            {
                ZabbixServerUrl = ZabbixServerUrl,
                ZabbixApiToken = ZabbixApiToken,
                TemplateGroupId = TemplateGroupId,
                SelectedItems = _appSettingsService.Settings.SelectedItems,
            };

            _appSettingsService.UpdateSettings(newSettings);

            await OnGetAsync();

            return Page();
        }
    }
}