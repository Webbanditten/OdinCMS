using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        [HousekeepingFilter(Fuse.housekeeping)]
        public IActionResult Index()
        {
            return View();
        }
    }
}
