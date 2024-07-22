using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Text;
using System.Text.RegularExpressions;

namespace zabbixreport.Services
{
    public class ZabbixApiClient
    {
        private readonly string _url;
        private readonly HttpClient _httpClient;
        private readonly string _authToken;

        public ZabbixApiClient(string url, string authToken)
        {
            _url = url;
            _authToken = authToken;
            _httpClient = new HttpClient();
        }

        public async Task<List<Template>> GetTemplatesInGroupAsync(int groupName)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "template.get",
                @params = new
                {
                    output = "extend",
                    groupids = groupName
                },
                id = 1,
                auth = _authToken
            };

            var response = await SendRequestAsync(request);
            return JsonConvert.DeserializeObject<List<Template>>(response.result.ToString());
        }

        public async Task<List<Trigger>> GetTriggersByTemplateIdAsync(string templateId)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "trigger.get",
                @params = new
                {
                    output = "extend",
                    templateids = templateId
                },
                id = 1,
                auth = _authToken
            };

            var response = await SendRequestAsync(request);
            return JsonConvert.DeserializeObject<List<Trigger>>(response.result.ToString());
        }

        public async Task<Dictionary<string, string>> GetItemsByTemplateIdAsync(string templateId)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "item.get",
                @params = new
                {
                    output = new[] { "itemid", "name" },
                    templateids = templateId
                },
                id = 1,
                auth = _authToken
            };

            var response = await SendRequestAsync(request);
            var items = JsonConvert.DeserializeObject<List<Item>>(response.result.ToString());

            var itemDict = new Dictionary<string, string>();
            foreach (var item in items)
            {
                itemDict[item.itemid] = item.name;
            }

            return itemDict;
        }

        public async Task<List<HostGroup>> GetAllHostGroupsAsync()
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "hostgroup.get",
                @params = new
                {
                    output = "extend"
                },
                id = 1,
                auth = _authToken
            };

            var response = await SendRequestAsync(request);
            return JsonConvert.DeserializeObject<List<HostGroup>>(response.result.ToString());
        }

        public async Task<List<Host>> GetHostsByGroupIdAsync(string groupId)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "host.get",
                @params = new
                {
                    output = new[] { "hostid", "name" },
                    groupids = groupId
                },
                id = 1,
                auth = _authToken
            };

            var response = await SendRequestAsync(request);
            return JsonConvert.DeserializeObject<List<Host>>(response.result.ToString());
        }

        public async Task<List<Item>> GetItemsByHostIdAsync(string hostId)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "item.get",
                @params = new
                {
                    output = new[] { "itemid", "name", "value_type" },
                    hostids = hostId
                },
                id = 1,
                auth = _authToken
            };

            var response = await SendRequestAsync(request);
            return JsonConvert.DeserializeObject<List<Item>>(response.result.ToString());
        }

        public async Task<List<History>> GetHistoryByItemIdAsync(string itemId, int valueType, long timeFrom, long timeTill)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "history.get",
                @params = new
                {
                    output = "extend",
                    itemids = itemId,
                    history = valueType,
                    time_from = timeFrom,
                    time_till = timeTill,
                    sortfield = "clock",
                    sortorder = "ASC"
                },
                id = 1,
                auth = _authToken
            };

            var response = await SendRequestAsync(request);
            return JsonConvert.DeserializeObject<List<History>>(response.result.ToString());
        }

        public async Task<List<Item>> GetItemsByIdsAsync(List<string> itemIds)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "item.get",
                @params = new
                {
                    itemids = itemIds.ToArray(),
                    output = "extend"
                },
                id = 1,
                auth = _authToken
            };

            var response = await SendRequestAsync(request);
            var items = JsonConvert.DeserializeObject<List<Item>>(response.result.ToString());
            return items;
        }

        public async Task<List<ItemPrototype>> GetItemPrototypesByTemplateIdAsync(string templateId)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "itemprototype.get",
                @params = new
                {
                    output = "extend",
                    templateids = templateId
                },
                id = 1,
                auth = _authToken
            };

            var response = await SendRequestAsync(request);
            var resultList = JsonConvert.DeserializeObject<List<ItemPrototype>>(response.result.ToString());
            return resultList;
        }

        public async Task<Item> GetItemByIdAsync(string itemId)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "item.get",
                @params = new
                {
                    output = "extend",
                    itemids = new[] { itemId } // Передаем itemids как массив
                },
                id = 1,
                auth = _authToken
            };

            var response = await SendRequestAsync(request);

            // Проверяем, что результат не пустой и содержит данные
            if (response.result == null || response.result.Count == 0)
            {
                //throw new KeyNotFoundException($"Item with ID {itemId} not found.");
                request = new
                {
                    jsonrpc = "2.0",
                    method = "itemprototype.get",
                    @params = new
                    {
                        output = "extend",
                        itemids = new[] { itemId } // Передаем itemids как массив
                    },
                    id = 1,
                    auth = _authToken
                };

                response = await SendRequestAsync(request);

                if (response.result == null || response.result.Count == 0)
                {
                    throw new KeyNotFoundException($"Item with ID {itemId} not found.");
                }
            }

            // Преобразуем результат в массив объектов
            var items = JsonConvert.DeserializeObject<List<Item>>(response.result.ToString());

            // Проверка наличия элементов в списке и возврат первого элемента
            if (items != null && items.Count > 0)
            {
                return items[0];
            }

            throw new KeyNotFoundException($"Item with ID {itemId} not found.");
        }

        //public async Task<Item> GetItemByIdAsync(string itemId)
        //{
        //    var request = new
        //    {
        //        jsonrpc = "2.0",
        //        method = "item.get",
        //        @params = new
        //        {
        //            output = "extend",
        //            itemids = new[] { itemId } // Используем массив, даже для одного элемента
        //        },
        //        id = 1,
        //        auth = _authToken
        //    };

        //    var response = await SendRequestAsync(request);
        //    var items = JsonConvert.DeserializeObject<List<Item>>(response.result.ToString());

        //    if (items == null || items.Count == 0)
        //    {
        //        throw new KeyNotFoundException($"Item with ID {itemId} not found.");
        //    }

        //    return items.FirstOrDefault();
        //}

        private async Task<dynamic> SendRequestAsync(object requestData)
        {
            var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_url, content);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<dynamic>(responseContent);
        }
    }

    public class HostGroup
    {
        public string groupid { get; set; }
        public string name { get; set; }
    }

    public class Template
    {
        public string templateid { get; set; }
        public string host { get; set; }
        public string name { get; set; }
    }

    public class Trigger
    {
        public string triggerid { get; set; }
        public string description { get; set; }
        public string priority { get; set; }
        public string expression { get; set; }
        public string recovery_mode { get; set; }
        public string recovery_expression { get; set; }
    }

    public class Item
    {
        public string itemid { get; set; }
        public string type { get; set; }
        public string snmp_oid { get; set; }
        public string hostid { get; set; }
        public string name { get; set; }
        public string key_ { get; set; }
        public string delay { get; set; }
        public string history { get; set; }
        public string trends { get; set; }
        public string status { get; set; }
        public int value_type { get; set; }
        public string trapper_hosts { get; set; }
        public string units { get; set; }
        public string formula { get; set; }
        public string logtimefmt { get; set; }
        public string templateid { get; set; }
        public string valuemapid { get; set; }
        public string @params { get; set; }
        public string ipmi_sensor { get; set; }
        public string authtype { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string publickey { get; set; }
        public string privatekey { get; set; }
        public string flags { get; set; }
        public string interfaceid { get; set; }
        public string description { get; set; }
        public string inventory_link { get; set; }
        public string lifetime { get; set; }
        public string evaltype { get; set; }
        public string jmx_endpoint { get; set; }
        public string master_itemid { get; set; }
        public string timeout { get; set; }
        public string url { get; set; }
        public List<object> query_fields { get; set; }
        public string posts { get; set; }
        public string status_codes { get; set; }
        public string follow_redirects { get; set; }
        public string post_type { get; set; }
        public string http_proxy { get; set; }
        public List<object> headers { get; set; }
        public string retrieve_mode { get; set; }
        public string request_method { get; set; }
        public string output_format { get; set; }
        public string ssl_cert_file { get; set; }
        public string ssl_key_file { get; set; }
        public string ssl_key_password { get; set; }
        public string verify_peer { get; set; }
        public string verify_host { get; set; }
        public string allow_traps { get; set; }
        public string uuid { get; set; }
        public string lifetime_type { get; set; }
        public string enabled_lifetime_type { get; set; }
        public string enabled_lifetime { get; set; }
        public string state { get; set; }
        public string error { get; set; }
        public string name_resolved { get; set; }
        public List<object> parameters { get; set; }
        public string lastclock { get; set; }
        public string lastns { get; set; }
        public string lastvalue { get; set; }
        public string prevvalue { get; set; }
    }
    public class Host
    {
        public string hostid { get; set; }
        public string name { get; set; }
    }

    public class History
    {
        public string itemid { get; set; }
        public string clock { get; set; }
        public string value { get; set; }
    }
    public class ItemPrototype
    {
        public string itemid { get; set; }
        public string name { get; set; }
        public string key_ { get; set; }
        public string delay { get; set; }
        public string status { get; set; }
        public string type { get; set; }
    }
}
