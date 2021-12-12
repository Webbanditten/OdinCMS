using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace KeplerCMS.Controllers
{
    [MaintenanceFilter]
    [MenuFilter]
    public class PageController : Controller
    {
        private readonly IPageService _pageService;

        public PageController(IPageService pageService)
        {
            _pageService = pageService;
        }

        [LoggedInFilter(false)]
        public async Task<IActionResult> Index(string slug, string subSlug = null)
        {
            var currentPath = HttpContext.Request.Path;
            if (currentPath.ToString() == "/")
            {
                return Redirect("~/home");
            }

            var dbSlug = slug;
            if(subSlug != null)
            {
                dbSlug = slug + "/" + subSlug;
            }
            var page = await _pageService.GetPageBySlug(dbSlug);

            if (page == null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return View("NotFound");
            }

            ViewData["page"] = page;
            return View();
        }
    }
}
