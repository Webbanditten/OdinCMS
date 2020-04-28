using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace KeplerCMS.Controllers
{
    public class PageController : Controller
    {
        private readonly IPageService _pageService;

        public PageController(IPageService pageService)
        {
            _pageService = pageService;
        }

        [LoggedInFilter(false)]
        public IActionResult Index(string slug, string subSlug = null)
        {
            var dbSlug = slug;
            if(subSlug != null)
            {
                dbSlug = slug + "/" + subSlug;
            }
            var page = _pageService.GetPageBySlug(dbSlug);

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
