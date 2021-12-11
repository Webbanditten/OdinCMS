using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;

namespace KeplerCMS.Controllers
{
    [MaintenanceFilter]
    [MenuFilter]
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;
        private readonly ISettingsService _settingService;

        public ProfileController(IUserService userService, ISettingsService settingService)
        {
            _settingService = settingService;
            _userService = userService;
        }

        [LoggedInFilter]
        public async Task<IActionResult> Index()
        {
            return View(await _settingService.GetAll());   
        }

        [LoggedInFilter]
        [HttpPost]
        public IActionResult UpdateFigure(string newGender, string figureData)
        {
            //if((newGender == "M" || newGender == "F") && figureData.Length == 25)
            //{
                _userService.UpdateFigure(User.Identity.Name, figureData, newGender);
            //}
            return RedirectToAction("index");
        }
    }
}
