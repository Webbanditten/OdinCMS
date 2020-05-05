using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;

namespace KeplerCMS.Controllers
{

    [MenuFilter]
    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        [LoggedInFilter(false)]
        public IActionResult Index()
        {
            var currentPath = HttpContext.Request.Path;
            if(!currentPath.ToString().Contains("/home"))
            {
                return Redirect("~/home");
            }

            return View();
        }
    }
}
