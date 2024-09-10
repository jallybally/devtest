using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using zabbixreport.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using zabbixreport.DBStruct;
using zabbixreport.Services;
using System.Globalization;

namespace zabbixreport.Pages;

public class CreateNomenclatureModel : PageModel
{
    private readonly NomenclatureService _nomenclatureService;
    private readonly MongoDBService _mongoDBService;

    [BindProperty]
    public Nomenclature Nomenclature { get; set; }

    [BindProperty]
    public List<string> ServiceTypes { get; set; }

    public string ServiceType { get; set; }

    public List<string> AvailableWorks { get; set; }


    public CreateNomenclatureModel(NomenclatureService nomenclatureService, MongoDBService mongoDBService)
    {
        _nomenclatureService = nomenclatureService;
        _mongoDBService = mongoDBService;
    }

    public async Task OnGetAsync()
    {
        Nomenclature = new Nomenclature
        {
            Works = new List<Work> { new Work() }  // Инициализируем список пустым элементом для ввода
        };

        // Получите типы услуг из коллекции Settings
        ServiceTypes = await _mongoDBService.GetServiceTypesAsync();

        // Получите список работ из коллекции Settings
        AvailableWorks = await _mongoDBService.GetWorksAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        
        if (!ModelState.IsValid)
        {
            foreach (var state in ModelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    Console.WriteLine($"Field: {state.Key}, Error: {error.ErrorMessage}");
                }
            }
        
            ServiceTypes = await _mongoDBService.GetServiceTypesAsync();
            AvailableWorks = await _mongoDBService.GetWorksAsync();
            return Page();
        }
        
        var culture = new CultureInfo("ru-RU");

        foreach (var work in Nomenclature.Works)
        {
            if (decimal.TryParse(work.Hours.ToString(), NumberStyles.Any, culture, out var hours))
            {
                work.Hours = hours;
            }
            else
            {
                ModelState.AddModelError(nameof(Nomenclature.Works), "Некорректное значение для количества часов.");
                return Page();
            }

            if (work.Quantity <= 0)
            {
                ModelState.AddModelError(nameof(Nomenclature.Works), "Количество должно быть больше нуля.");
                return Page();
            }
        }

        await _nomenclatureService.CreateAsync(Nomenclature);

        return RedirectToPage("NomenclatureIndex");
    }
}
