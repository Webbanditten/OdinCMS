using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;

namespace KeplerCMS.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;

        public ProfileController(IUserService userService)
        {
            _userService = userService;
        }

        [LoggedInFilter]
        public IActionResult Index()
        {
            return View();
            
        }
        [HttpPost]
        public IActionResult UpdateFigure(string newGender, string figureData)
        {
            if((newGender == "M" || newGender == "F") && figureData.Length == 25)
            {
                _userService.UpdateFigure(User.Identity.Name, figureData, newGender);
            }
            return RedirectToAction("index");
        }
    }
}
