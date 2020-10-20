using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Services.Interfaces;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class CatalogController : Controller
    {
        private readonly ICommandQueueService _commandQueueService;

        public CatalogController(ICommandQueueService commandQueueService)
        {
            _commandQueueService = commandQueueService;
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public IActionResult Index(string message = null)
        {
            ViewData["message"] = message;
            return View();
        }
    }
}
