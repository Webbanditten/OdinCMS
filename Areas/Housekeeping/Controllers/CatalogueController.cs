using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Services.Interfaces;
using System.Collections.Generic;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class CatalogueController : Controller
    {
        private readonly ICommandQueueService _commandQueueService;
        private readonly ICatalogueService _catalogueService;

        public CatalogueController(ICommandQueueService commandQueueService, ICatalogueService catalogueService)
        {
            _commandQueueService = commandQueueService;
            _catalogueService = catalogueService;
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> IndexAsync(string message = null)
        {
            var pages = await _catalogueService.GetCataloguePages();
            ViewData["pages"] = pages;
            ViewData["message"] = message;
            return View();
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        [HttpPost]
        public async Task<IActionResult> Index([FromBody] List<CatalogueReArrangeModel> model)
        {
            await _catalogueService.ReArrange(model);
            return Content("OK");
        }
    }
}
