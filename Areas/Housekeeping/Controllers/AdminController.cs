using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class AdminController : Controller
    {
        public AdminController()
        {
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public IActionResult Index()
        {
            return View();
        }
    }
}
