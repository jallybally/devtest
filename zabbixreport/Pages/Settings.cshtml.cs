using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using zabbixreport.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace zabbixreport.Pages
{
    public class SettingsModel : PageModel
    {
        private readonly MongoDBService _mongoDBService;
        private readonly ZabbixApiClient _zabbixApiClient;

        public SettingsModel(MongoDBService mongoDBService, ZabbixApiClient zabbixApiClient)
        {
            _mongoDBService = mongoDBService;
            _zabbixApiClient = zabbixApiClient;
        }

        [BindProperty]
        public List<string> ServiceTypes { get; set; } = new List<string>();

        [BindProperty]
        public List<string> Works {get; set; } = new List<string>();

        [BindProperty]
        public string ZabbixServerUrl { get; set; }

        [BindProperty]
        public string ZabbixApiToken { get; set; }

        [BindProperty]
        public int TemplateGroupId { get; set; }

        [BindProperty]
        public List<string> SelectedItems { get; set; }

        public List<Template> Templates { get; set; }

        public Dictionary<string, Dictionary<string, string>> TemplateItems { get; set; }

        public Dictionary<string, List<ItemPrototype>> TemplatePrototypes { get; set; }

        [BindProperty]
        public Dictionary<string, bool> SelectedItemsState { get; set; }

        public async Task OnGetAsync()
        {
            var settings = await _mongoDBService.GetSettingsAsync();

            if (settings == null)
            {
                settings = new AppSettings();
            }

            SelectedItems = settings.SelectedItems ?? new List<string>();
            SelectedItemsState = new Dictionary<string, bool>();

            ZabbixServerUrl = settings.ZabbixServerUrl;
            ZabbixApiToken = settings.ZabbixApiToken;
            TemplateGroupId = settings.TemplateGroupId;

            // Получаем шаблоны из Zabbix API
            Templates = await _zabbixApiClient.GetTemplatesInGroupAsync(TemplateGroupId);
            TemplateItems = new Dictionary<string, Dictionary<string, string>>();
            TemplatePrototypes = new Dictionary<string, List<ItemPrototype>>();

            foreach (var template in Templates)
            {
                var items = await _zabbixApiClient.GetItemsByTemplateIdAsync(template.templateid);
                TemplateItems[template.templateid] = items;

                var prototypes = await _zabbixApiClient.GetItemPrototypesByTemplateIdAsync(template.templateid);
                TemplatePrototypes[template.templateid] = prototypes;

                foreach (var item in items)
                {
                    SelectedItemsState[item.Key] = SelectedItems.Contains(item.Key);
                }

                foreach (var prototype in prototypes)
                {
                    SelectedItemsState[prototype.itemid] = SelectedItems.Contains(prototype.itemid);
                }
            }


            // Загрузка существующих типов услуг и других настроек
            await LoadServiceTypes();
            await LoadWorks();
        }

        /*
        public async Task<IActionResult> OnPostAsync(string action)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (action == "UpdateAccounting")
            {
                // Обработка данных из вкладки "Настройки учёта"
                var updatedServiceTypes = Request.Form["ServiceTypes"].ToList();
                ServiceTypes = updatedServiceTypes;

                // Сохранение в базе данных
                var settings = await _mongoDBService.GetSettingsAsync();

                if (settings != null)
                {
                    settings.ServiceTypes = ServiceTypes;
                    await _mongoDBService.SaveSettingsAsync(settings);
                }
                else
                {
                    settings = new AppSettings
                    {
                        ServiceTypes = ServiceTypes
                    };
                    await _mongoDBService.SaveSettingsAsync(settings);
                }

                // Перезагрузка данных после сохранения
                await LoadServiceTypes();

                // Перезагрузка страницы с активной вкладкой "настройки учёта"
                return RedirectToPage(new { tab = "accounting" });
            }

            // Обработка данных из вкладки "Настройки мониторинга"
            var selectedItems = SelectedItemsState
                .Where(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            var newSettings = new AppSettings
            {
                ZabbixServerUrl = ZabbixServerUrl,
                ZabbixApiToken = ZabbixApiToken,
                TemplateGroupId = TemplateGroupId,
                SelectedItems = selectedItems
            };

            await _mongoDBService.SaveSettingsAsync(newSettings);

            // Перезагрузка страницы с активной вкладкой "настройки мониторинга"
            return RedirectToPage(new { tab = "monitoring" });
        }
        */

        private async Task LoadServiceTypes()
        {
            var settings = await _mongoDBService.GetSettingsAsync();

            if (settings != null)
            {
                ServiceTypes = settings.ServiceTypes ?? new List<string>();
            }
            else
            {
                ServiceTypes = new List<string>();
            }
        }

        private async Task LoadWorks()
        {
            var settings = await _mongoDBService.GetSettingsAsync();

            if (settings != null)
            {
                Works = settings.Works ?? new List<string>();
            }
            else
            {
                Works = new List<string>();
            }
        }
        public async Task<IActionResult> OnPostAsync(string tab)
        {
            
            if (!ModelState.IsValid)
            {
                // Возвращаемся на ту же вкладку, если данные недействительны
                return RedirectToPage(new { tab });
            }

            // Получаем текущие настройки из базы данных
            var settings = await _mongoDBService.GetSettingsAsync();

            // Обновляем настройки на основе полученных данных
            settings.ZabbixServerUrl = ZabbixServerUrl;
            settings.ZabbixApiToken = ZabbixApiToken;
            settings.TemplateGroupId = TemplateGroupId;

            var selectedItems = SelectedItemsState
                .Where(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            settings.SelectedItems = selectedItems;
            settings.ServiceTypes = ServiceTypes;
            settings.Works = Works;

            // Сохраняем обновлённые настройки обратно в базу данных
            await _mongoDBService.SaveSettingsAsync(settings);

            // Возвращаемся на ту же вкладку после обновления
            return RedirectToPage(new { tab });
        }

    }
}
