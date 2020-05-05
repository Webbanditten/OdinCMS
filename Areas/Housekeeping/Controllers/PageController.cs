using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class PageController : Controller
    {
        public PageController()
        {
        }

        [LoggedInFilter(false)]
        public IActionResult Index()
        {
            return View();
        }
    }
}
