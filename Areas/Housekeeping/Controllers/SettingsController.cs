using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;
using KeplerCMS.Models.Enums;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;
        private readonly ICommandQueueService _commandQueue;

        public SettingsController(ISettingsService settingsService, ICommandQueueService commandQueue)
        {
            _settingsService = settingsService;
            _commandQueue = commandQueue;
        }
        
        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> InstallMissing()
        {
            await _settingsService.InstallDefaultSettings();
            return RedirectToAction("Index", "Settings", new { message = "Default settings installed." });
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Index(string message)
        {
            ViewData["message"] = message;
            return View(await _settingsService.GetAll());
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

                // The game server shares this table (every setting not starting with "cms."): it reloads its settings
                // straight away and refreshes the club badges of everyone online (Odin's reload_settings).
                if (!model.Setting.StartsWith("cms."))
                {
                    _commandQueue.QueueCommand(CommandQueueType.reload_settings, new CommandTemplate());
                    return RedirectToAction("Index", "Settings", new { message = "Setting updated. The game server has reloaded its settings." });
                }
                return RedirectToAction("Index", "Settings", new { message = "Setting updated" });
            }
            return View(model);
        }
    }
}
