using System.Threading.Tasks;
using DataTables.Models;
using DataTables.Models.Configuration;
using DataTables.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataTables.Controllers
{
    public class ProductsController : Controller
    {
        public IActionResult Index(DataTablesConfiguration configuration)
        {
            return View(configuration);
        }

        [HttpPost]
        public async Task<IActionResult> IndexData(DataTablesInputModel inputModel, [FromServices] DataTablesService dataTablesService)
        {
            var result = await dataTablesService.GetResultsAsync(inputModel);
            return Json(result);
        }
    }
}