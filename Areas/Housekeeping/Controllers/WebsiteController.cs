using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class WebsiteController : Controller
    {
        public WebsiteController()
        {
        }

        [LoggedInFilter(false)]
        public IActionResult Index()
        {
            return View();
        }
    }
}
