using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class WebsiteController : Controller
    {
        public WebsiteController()
        {
        }

        [HousekeepingFilter(Fuse.housekeeping_website)]
        public IActionResult Index()
        {
            return View();
        }
    }
}
