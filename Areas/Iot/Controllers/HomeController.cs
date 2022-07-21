using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Iot
{
    [Area("Iot")]
    public class HomeController : Controller
    {
        public HomeController()
        {
           
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }
    }
}
