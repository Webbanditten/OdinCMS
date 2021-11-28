using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class HomeController : Controller
    {
        ISettingsService _settingsService;
        public HomeController(ISettingsService settingsService)
        {
            this._settingsService = settingsService;
        }

        [HousekeepingFilter(Fuse.housekeeping)]
        public async Task<IActionResult> Index()
        {
            return View(await _settingsService.GetMissingDefaultSettings());
        }
    }
}
