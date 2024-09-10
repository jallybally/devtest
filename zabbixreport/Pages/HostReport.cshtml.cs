using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using zabbixreport.Services;

namespace zabbixreport.Pages
{
    public class HostReportModel : PageModel
    {
        private readonly ZabbixApiClient _zabbixApiClient;

        public HostReportModel(ZabbixApiClient zabbixApiClient)
        {
            _zabbixApiClient = zabbixApiClient;
        }

        public Dictionary<string, List<Services.Host>> GroupHosts { get; set; }

        public async Task OnGetAsync()
        {
            var allHostGroups = await _zabbixApiClient.GetAllHostGroupsAsync();
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

            var selectedGroups = allHostGroups.Where(g => !excludedGroups.Contains(g.name)).ToList();

            GroupHosts = new Dictionary<string, List<Services.Host>>();

            foreach (var group in selectedGroups)
            {
                var hosts = await _zabbixApiClient.GetHostsByGroupIdAsync(group.groupid);
                GroupHosts[group.name] = hosts;
            }
        }

        public IActionResult OnPost()
        {
            var groupName = Request.Form["GroupName"]; ;
            var startDate = Request.Form["StartDate"]; ;
            var endDate = Request.Form["EndDate"]; ;

            if (string.IsNullOrEmpty(groupName))
            {
                // Handle invalid input case
                return BadRequest("Please provide all required fields.");
            }

            if (groupName == "all")
            {
                // Логика для обработки отчета по всем группам
                return RedirectToPage("/GroupReport", new { groupName = "all", startDate = startDate, endDate = endDate });
            }
            else
            {
                // Redirect to GroupReport page with parameters
                return RedirectToPage("/GroupReport", new { groupName = groupName, startDate = startDate, endDate = endDate });
            }
        }
    }
}
