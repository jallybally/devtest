using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using zabbixreport.Services;

namespace zabbixreport.Pages
{
    public class GroupReportModel : PageModel
    {
        private readonly ZabbixApiClient _zabbixApiClient;
        private readonly AppSettingsService _appSettingsService;

        public GroupReportModel(ZabbixApiClient zabbixApiClient, AppSettingsService appSettingsService)
        {
            _zabbixApiClient = zabbixApiClient;
            _appSettingsService = appSettingsService;
        }

        [BindProperty(SupportsGet = true)]
        public string GroupName { get; set; }
        [BindProperty(SupportsGet = true)]
        public string StartDate { get; set; }
        [BindProperty(SupportsGet = true)]
        public string EndDate { get; set; }

        public List<Services.Host> Hosts { get; set; }
        public Dictionary<string, List<Item>> HostItems { get; set; }
        public Dictionary<string, Dictionary<string, (object value, string status)>> HostMetrics { get; set; }

        public string ReportPeriod { get; private set; }

        private static readonly Dictionary<int, string> ServiceStateMapping = new Dictionary<int, string>
        {
            { 0, "Running" },
            { 1, "Paused" },
            { 2, "Start pending" },
            { 3, "Pause pending" },
            { 4, "Continue pending" },
            { 5, "Stop pending" },
            { 6, "Stopped" },
            { 7, "Unknown" },
            { 255, "No such service" }
        };

        public async Task<IActionResult> OnGetAsync()
        {
            if (!DateTimeOffset.TryParse(StartDate, out DateTimeOffset startDateTimeOffset) ||
                !DateTimeOffset.TryParse(EndDate, out DateTimeOffset endDateTimeOffset))
            {
                return BadRequest("Invalid date format.");
            }

            ReportPeriod = $"{startDateTimeOffset:dd.MM.yyyy HH:mm} - {endDateTimeOffset:dd.MM.yyyy HH:mm}";

            long timeFrom = startDateTimeOffset.ToUnixTimeSeconds();
            long timeTill = endDateTimeOffset.ToUnixTimeSeconds();

            var selectedItems = _appSettingsService.Settings.SelectedItems;
            var selectedItemNames = await GetSelectedItemNamesAsync(selectedItems);

            if (GroupName == "all")
            {
                var excludedGroups = new HashSet<string>
                {
                    "Applications",
                    "Databases",
                    "Discovered hosts",
                    "Hypervisors",
                    "Linux servers",
                    "Virtual machines",
                    "Zabbix servers"
                };

                var allHostGroups = await _zabbixApiClient.GetAllHostGroupsAsync();
                var selectedGroups = allHostGroups.Where(g => !excludedGroups.Contains(g.name)).ToList();
                Hosts = new List<Services.Host>();

                foreach (var group in selectedGroups)
                {
                    var groupHosts = await _zabbixApiClient.GetHostsByGroupIdAsync(group.groupid);
                    Hosts.AddRange(groupHosts);
                }
            }
            else
            {
                var allHostGroups = await _zabbixApiClient.GetAllHostGroupsAsync();
                var group = allHostGroups.FirstOrDefault(g => g.name == GroupName);

                if (group != null)
                {
                    Hosts = await _zabbixApiClient.GetHostsByGroupIdAsync(group.groupid);
                }
            }

            if (Hosts != null)
            {
                HostItems = new Dictionary<string, List<Item>>();
                HostMetrics = new Dictionary<string, Dictionary<string, (object value, string status)>>();

                foreach (var host in Hosts)
                {
                    var items = await _zabbixApiClient.GetItemsByHostIdAsync(host.hostid);
                    var filteredItems = items.Where(i => IsItemSelected(i.name, selectedItemNames)).ToList();
                    HostItems[host.hostid] = filteredItems;

                    double cpuCores = 0;
                    var itemHistory = new Dictionary<string, List<History>>();

                    // First pass: collect data
                    foreach (var item in filteredItems)
                    {
                        var history = await _zabbixApiClient.GetHistoryByItemIdAsync(item.itemid, item.value_type, timeFrom, timeTill);
                        if (history.Count > 0)
                        {
                            itemHistory[item.name] = history;

                            if (item.name.Contains("ЦПУ количество ядер"))
                            {
                                var lastEntry = history.Last();
                                if (double.TryParse(lastEntry.value, out double cores))
                                {
                                    cpuCores = cores;
                                }
                            }
                        }
                    }

                    // Second pass: calculate metrics
                    foreach (var item in filteredItems)
                    {
                        if (itemHistory.ContainsKey(item.name))
                        {
                            var history = itemHistory[item.name];
                            if (item.name.Contains("Состояние службы"))
                            {
                                var serviceStates = history.Select(h => int.TryParse(h.value, out int state) && ServiceStateMapping.ContainsKey(state)
                                                                        ? ServiceStateMapping[state]
                                                                        : "Unknown")
                                                           .GroupBy(s => s)
                                                           .OrderByDescending(g => g.Count())
                                                           .FirstOrDefault()?.Key ?? "Unknown";

                                var metricStatus = DetermineMetricStatus(serviceStates, item.name, 0);

                                if (!HostMetrics.ContainsKey(host.hostid))
                                {
                                    HostMetrics[host.hostid] = new Dictionary<string, (object value, string status)>();
                                }
                                HostMetrics[host.hostid][item.name] = (serviceStates, metricStatus);
                            }
                            else if (item.name.Contains("ЦПУ количество ядер"))
                            {
                                var lastEntry = history.Last();
                                if (double.TryParse(lastEntry.value, out double cores))
                                {
                                    cpuCores = cores;
                                }
                            }
                            else if (item.value_type == 0 || item.value_type == 3)
                            {
                                List<double> values = new List<double>();

                                foreach (var entry in history)
                                {
                                    //if (double.TryParse(entry.value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result) ||
                                    //    int.TryParse(entry.value, NumberStyles.Any, CultureInfo.InvariantCulture, out int intResult))
                                    //{
                                    //    values.Add(result); // `int` значения автоматически преобразуются в `double`
                                    //}

                                    // Пробуем сначала как double
                                    if (double.TryParse(entry.value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                                    {
                                        values.Add(result);
                                    }
                                    // Если не получилось, пробуем как int и преобразуем в double
                                    else if (int.TryParse(entry.value, NumberStyles.Any, CultureInfo.InvariantCulture, out int intResult))
                                    {
                                        values.Add(intResult);
                                    }
                                }

                                if (values.Count > 0)
                                {
                                    values.Sort();
                                    //double percentile95 = Math.Round(values[(int)(values.Count * 0.95)]);
                                    double percentile95 = Math.Round(CalculatePercentile(values, 0.95));

                                    if (!HostMetrics.ContainsKey(host.hostid))
                                    {
                                        HostMetrics[host.hostid] = new Dictionary<string, (object value, string status)>();
                                    }

                                    if (item.name.Contains("ОЗУ") && !item.name.Contains('%'))
                                    {
                                        percentile95 = Math.Round(percentile95 / (1024 * 1024 * 1024), 2); // Convert bytes to GB
                                    }
                                    else if (item.name.Contains("FS") && !item.name.Contains('%'))
                                    {
                                        percentile95 = Math.Round(percentile95 / (1024 * 1024 * 1024), 2); // Convert bytes to GB
                                    }

                                    var metricStatus = DetermineMetricStatus(percentile95, item.name, cpuCores);
                                    if (item.name.Contains("%") || item.name.Contains("утилизация"))
                                    {
                                        HostMetrics[host.hostid][item.name] = ($"{percentile95}%", metricStatus);
                                    }
                                    else
                                    {
                                        HostMetrics[host.hostid][item.name] = (percentile95, metricStatus);
                                    }
                                }
                            }
                            else // Non-numeric values
                            {
                                var lastValue = history.Last().value;

                                if (!HostMetrics.ContainsKey(host.hostid))
                                {
                                    HostMetrics[host.hostid] = new Dictionary<string, (object value, string status)>();
                                }

                                HostMetrics[host.hostid][item.name] = (lastValue, "normal");
                            }
                        }
                    }
                }
            }
            return Page();
        }

        private double CalculatePercentile(List<double> sequence, double percentile)
        {
            if (sequence.Count == 0)
                return 0.0;

            sequence.Sort();
            double realIndex = percentile * (sequence.Count - 1);
            int index = (int)realIndex;
            double frac = realIndex - index;

            if (index + 1 < sequence.Count)
            {
                return sequence[index] * (1 - frac) + sequence[index + 1] * frac;
            }
            else
            {
                return sequence[index];
            }
        }

        private async Task<List<string>> GetSelectedItemNamesAsync(List<string> selectedItemIds)
        {
            var selectedItemNames = new List<string>();

            foreach (var itemId in selectedItemIds)
            {
                var item = await _zabbixApiClient.GetItemByIdAsync(itemId);
                selectedItemNames.Add(item.name);
            }

            return selectedItemNames;
        }

        private bool IsItemSelected(string itemName, List<string> selectedItemNames)
        {
            foreach (var pattern in selectedItemNames)
            {
                // Преобразуем шаблон {#VARIABLE} в .*
                var regexPattern = "^" + ReplaceVariablesWithWildcard(pattern) + "$";

                // Проверка совпадения с использованием регулярного выражения
                if (Regex.IsMatch(itemName, regexPattern))
                {
                    return true;
                }
            }
            return false;
        }

        private string ReplaceVariablesWithWildcard(string pattern)
        {
            // Найдем все подстроки вида {#VARIABLE} и заменим их на .*
            //var regex = new Regex(@"\{\#\w+\}");
            //var result = regex.Replace(pattern, ".*");

            var regex = new Regex(@"\{\#\w+(\.\w+)?\}");
            var result = regex.Replace(pattern, ".*");

            // Экранируем все специальные символы кроме .*
            result = Regex.Escape(result).Replace(@"\.\*", ".*");

            return result;
        }

        private string DetermineMetricStatus(object value, string metricName, double cpuCores)
        {
            if (value is double doubleValue)
            {
                if (metricName.Contains("CPU"))
                {
                    if (doubleValue > 95)
                    {
                        return "high";
                    }
                    else if (doubleValue > 90)
                    {
                        return "medium";
                    }
                    else
                    {
                        return "low";
                    }
                }
                else if (metricName.Contains("ЦПУ длинна очереди"))
                {
                    if (doubleValue == cpuCores)
                    {
                        return "medium";
                    }
                    else if (doubleValue > cpuCores)
                    {
                        return "high";
                    }
                    else
                    {
                        return "normal";
                    }
                }
                else if (metricName.Contains("ОЗУ утилизация"))
                {
                    if (doubleValue > 95)
                    {
                        return "medium";
                    }
                    else if (doubleValue > 90)
                    {
                        return "high";
                    }
                    else
                    {
                        return "normal";
                    }
                }
                else if (metricName.Contains("ЦПУ iowait"))
                {
                    if (doubleValue > 20)
                    {
                        return "medium";
                    }
                    else if (doubleValue > 10)
                    {
                        return "high";
                    }
                    else
                    {
                        return "normal";
                    }
                }
                else if (metricName.Contains("Занято места %"))
                {
                    if (doubleValue > 90)
                    {
                        return "medium";
                    }
                    else if (doubleValue > 80)
                    {
                        return "high";
                    }
                    else
                    {
                        return "normal";
                    }
                }
            }
            else if (value is string stringValue && metricName.Contains("Состояние службы"))
            {
                if (stringValue == "Running")
                {
                    return "low";
                }
                else if (stringValue == "Stopped" || stringValue == "Stop pending")
                {
                    return "high";
                }
                else
                {
                    return "normal";
                }
            }
            return "normal";
        }

    }
}