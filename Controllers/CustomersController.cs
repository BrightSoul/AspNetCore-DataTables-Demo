using System.Threading.Tasks;
using DataTables.Models;
using DataTables.Models.Configuration;
using DataTables.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataTables.Controllers
{
    public class CustomersController : Controller
    {
        public IActionResult Index(DataTablesConfiguration configuration)
        {
            return View(configuration);
        }

        //Quest'action fornisce i dati a DataTables e deve chiamarsi come l'action a cui si riferisce + il suffisso Data
        //Dato che si riferisce alla DataTables presentata in Index, ecco che il suo nome deve essere IndexData
        public async Task<IActionResult> IndexData(DataTablesInputModel inputModel, [FromServices] DataTablesService dataTablesService)
        {
            var result = await dataTablesService.GetResultsAsync(inputModel);
            return Json(result);
        }
    }
}
