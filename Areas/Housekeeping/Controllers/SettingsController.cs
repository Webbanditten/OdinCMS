using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Index()
        {
            return View(await _settingsService.GetAll());
        }


        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public IActionResult Create()
        {
            return View();
        }

        
        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Update(string id)
        {
            var upload = await _settingsService.Get(id);
            return View(upload);
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        [HttpPost]
        public async Task<IActionResult> Update(Settings model)
        {
            if (ModelState.IsValid)
            {
                await _settingsService.Update(model);
                return RedirectToAction("Index", "Settings", new { message = "Setting updated" });
            }
            return View(model);
        }
    }
}
