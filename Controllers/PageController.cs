using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Controllers
{
    [MaintenanceFilter]
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
            // The edit mode (wwwroot/js/cms-edit.js): staff who may edit pages get the edit button; with the cms_edit
            // cookie set they see every box, hidden and scheduled ones too, with tools to edit, move, hide and add.
            var user = ViewData["user"] as KeplerCMS.Data.Models.Users;
            var canEdit = user?.Fuses?.Any(f => f.FuseName == nameof(KeplerCMS.Models.Fuse.housekeeping_pages)) == true;
            var editing = canEdit && Request.Cookies["cms_edit"] == "1";
            ViewData["cmsCanEdit"] = canEdit;
            ViewData["cmsEdit"] = editing;
            var page = await _pageService.GetPageBySlug(dbSlug, editing);

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
