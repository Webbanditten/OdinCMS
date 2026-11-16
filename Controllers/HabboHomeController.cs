using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace KeplerCMS.Controllers
{
    [MaintenanceFilter]
    public class HabboHomeController : Controller
    {
        private readonly IUserService _userService;
        private readonly IHomeService _homeService;
        private readonly IFriendService _friendService;
        public HabboHomeController(IUserService userService, IHomeService homeService, IFriendService friendService)
        {
            _userService = userService;
            _homeService = homeService;
            _friendService = friendService;
        }

        [Route("home/{username:minlength(1)}")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> Index(string username)
        {
            var habboHomeUser = await _userService.GetUserByUsername(username);
            if (habboHomeUser == null) return View("HomeNotFound");

            int? currentUserId = int.TryParse(User.Identity?.Name, out var id) ? id : null;
            int? editId = int.TryParse(Request.Cookies["editid"], out var cookieId) ? cookieId : null;

            var home = await _homeService.GetHome(habboHomeUser.Id, editId != null, currentUserId);
            if (home == null) return View("HomeNotFound");

            // Edit mode only on your own home, for the home the cookie names.
            if (home.IsEditing && (currentUserId != habboHomeUser.Id || editId != home.Home.Id))
            {
                Response.Cookies.Delete("editid");
                home.IsEditing = false;
                home.Items.ForEach(i => i.EnableEditing = false);
            }
            if (currentUserId != null)
            {
                ViewData["canFriend"] = await _friendService.IsFriends(currentUserId.Value, habboHomeUser.Id);
            }
            return View(home);
            
        }



        [Route("home/{id}/id")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> HomeById(int id)
        {
            var habboHomeUser = await _userService.GetUserById(id);
            if(habboHomeUser != null)
            {
                return Redirect("~/home/" + habboHomeUser.Username);
            }
            return View("HomeNotFound");

        }

        [Route("home/edit/{homeId}")]
        [LoggedInFilter]
        public async Task<IActionResult> Edit(int homeId)
        {
            var currentUser = await _userService.GetUserById(int.Parse(User.Identity.Name));
            var home = await _homeService.GetHomeDetailsById(homeId);
            if (currentUser != null && home != null && home.Type == "user" && home.UserId == currentUser.Id)
            {
                // Set editing mode
                CookieOptions option = new CookieOptions
                {
                    Expires = DateTime.Now.AddMinutes(30)
                };
                Response.Cookies.Append("editid", homeId.ToString(), option);

                return Redirect("/home/" + currentUser.Username);
            }

            return Redirect("/home");
        }


        [Route("home/cancel/{homeId}")]
        [LoggedInFilter]
        public async Task<IActionResult> CancelEditing(int homeId)
        {
            var home = await _homeService.GetHomeDetailsById(homeId);
            var homeUser = home == null ? null : await _userService.GetUserById(home.UserId);
            Response.Cookies.Delete("editid");
            return Redirect(homeUser == null ? "/home" : "/home/" + homeUser.Username);
        }


    }
}
