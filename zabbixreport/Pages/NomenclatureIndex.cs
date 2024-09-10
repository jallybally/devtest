using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using zabbixreport.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using zabbixreport.DBStruct;

namespace zabbixreport.Pages
{
    public class NomenclatureIndexModel : PageModel
    {
        private readonly NomenclatureService _nomenclatureService;
    
        public List<Nomenclature> Nomenclatures { get; set; }
    
        public NomenclatureIndexModel(NomenclatureService nomenclatureService)
        {
            _nomenclatureService = nomenclatureService;
        }
    
        public async Task OnGetAsync()
        {
            Nomenclatures = await _nomenclatureService.GetAllAsync();
        }
    }
}