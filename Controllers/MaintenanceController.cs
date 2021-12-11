using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Threading.Tasks;
using KeplerCMS.Services.Interfaces;
using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Controllers
{
    public class MaintenanceController : Controller
    {
        public MaintenanceController(IHomeService homeService, IUserService userService)
        {
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View();
        }
    }
}