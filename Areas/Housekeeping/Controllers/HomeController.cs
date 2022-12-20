using System.Collections.Generic;
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
    public class HomeController : Controller
    {
        ISettingsService _settingsService;
        IUserService _userService;

        public HomeController(ISettingsService settingsService, IUserService userService)
        {
            _settingsService = settingsService;
            _userService = userService;
        }

        [HousekeepingFilter(Fuse.housekeeping)]
        public async Task<IActionResult> Index()
        {
            var missingSettings = await _settingsService.GetMissingDefaultSettings();
            var latestSignins = await _userService.GetLatestSignins(15, 0);
            var monthlySignups = await _userService.GetMonthlySignups();
            var latestSignups = await _userService.GetLatestSignups(15, 0);
            var totalUsers = await _userService.TotalUsers();
            var totalSignedinUsers = await _userService.TotalSignedinUsers();

            return View(new HousekeepingHomeViewModels
            {
                MissingSettings = missingSettings,
                LatestSignins = latestSignins,
                MonthlySignups = monthlySignups,
                LatestSignups = latestSignups,
                totalUsers = totalUsers,
                totalSignedinUsers = totalSignedinUsers
            });
        }
    }
}
