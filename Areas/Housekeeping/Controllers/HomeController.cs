using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;

namespace KeplerCMS.Areas.Admin
{
    [Area("Housekeeping")]
    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        [LoggedInFilter(false)]
        public IActionResult Index()
        {
            return View();
        }
    }
}
