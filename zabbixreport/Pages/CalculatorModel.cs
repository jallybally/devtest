using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using zabbixreport.DBStruct;
using zabbixreport.Services;

namespace zabbixreport.Pages
{
    public class CalculatorModel : PageModel
    {
        private readonly NomenclatureService _nomenclatureService;

        public List<Nomenclature> Nomenclatures { get; set; }

        public CalculatorModel(NomenclatureService nomenclatureService)
        {
            _nomenclatureService = nomenclatureService;
        }

        public async Task OnGetAsync()
        {
            // Получаем все номенклатуры
            Nomenclatures = await _nomenclatureService.GetAllAsync();
        }
    }
}
