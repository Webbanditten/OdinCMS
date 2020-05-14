using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace KeplerCMS.Controllers
{
    [MenuFilter]
    public class HabboHomeController : Controller
    {
        private readonly IUserService _userService;
        private readonly IHomeService _homeService;

        public HabboHomeController(IUserService userService, IHomeService homeService)
        {
            _userService = userService;
            _homeService = homeService;
        }

        [Route("home/{username:minlength(1)}")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> Index(string username)
        {
            var habboHomeUser = await _userService.GetUserByUsername(username);
            var enableEditing = false;
            if(User.Identity.IsAuthenticated && User.Identity.Name == habboHomeUser.Id.ToString())
            {
                if(Request.Cookies["editmode"] != null && Request.Cookies["editmode"] == "true")
                {
                    enableEditing = true;
                }
            }
            var home = await _homeService.GetHome(habboHomeUser.Id, enableEditing);
            return View(home);
            
        }

        [Route("home/edit/{homeId}")]
        [LoggedInFilter]
        public async Task<IActionResult> Edit(int homeId)
        {
            var currentUser = await _userService.GetUserById(User.Identity.Name);
            var home = await _homeService.GetHomeDetailsById(homeId);
            if(home.UserId == currentUser.Id)
            {
                // Set editing mode
                CookieOptions option = new CookieOptions
                {
                    Expires = DateTime.Now.AddMinutes(30)
                };
                Response.Cookies.Append("editmode", "true", option);

                return Redirect("/home/" + currentUser.Username);
            }

            return Redirect("/home");
        }

        [Route("home/cancel/{homeId}")]
        [LoggedInFilter]
        public async Task<IActionResult> CancelEditing(int homeId)
        {
            var home = await _homeService.GetHomeDetailsById(homeId);
            var homeUser = await _userService.GetUserById(home.UserId.ToString());
            Response.Cookies.Delete("editmode");
            return Redirect("/home/" + homeUser.Username);
        }


    }
}
