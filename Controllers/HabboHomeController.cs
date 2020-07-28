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
            var enableEditing = false;
            if(habboHomeUser != null)
            {
                if (User.Identity.IsAuthenticated && User.Identity.Name != habboHomeUser.Id.ToString())
                {
                    Response.Cookies.Delete("editmode");
                }
                else
                {
                    if (Request.Cookies["editmode"] != null && Request.Cookies["editmode"] == "true")
                    {
                        enableEditing = true;
                    }
                }
                var home = await _homeService.GetHome(habboHomeUser.Id, enableEditing);

                if (home != null)
                {
                    if(User.Identity.IsAuthenticated)
                    {
                        ViewData["canFriend"] = await _friendService.IsFriends(int.Parse(User.Identity.Name), habboHomeUser.Id);
                    }
                    return View(home);
                }
            }
            return View("HomeNotFound");
            
        }

        [Route("groups/{groupname:minlength(1)}")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> Group(string groupname)
        {
            var enableEditing = false;
            if (Request.Cookies["editgroup"] != null && Request.Cookies["editgroup"] == "true")
            {
                enableEditing = true;
            }


            var group = await _homeService.GetHomeByGroupName(groupname, int.Parse(User.Identity.Name), enableEditing);
            if (group != null)
            {
                if (User.Identity.IsAuthenticated && User.Identity.Name != group.Home.UserId.ToString())
                {
                    Response.Cookies.Delete("editgroup");
                }

                return View(group);
            }
            return View("HomeNotFound");

        }

        [Route("groups/{id}/id")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> GroupById(int id)
        {
            var enableEditing = false;
            if (Request.Cookies["editgroup"] != null && Request.Cookies["editgroup"] == "true")
            {
                enableEditing = true;
            }


            var group = await _homeService.GetHomeByGroupId(id, int.Parse(User.Identity.Name), enableEditing);
            if (group != null)
            {
                if (User.Identity.IsAuthenticated && User.Identity.Name != group.Home.UserId.ToString())
                {
                    Response.Cookies.Delete("editgroup");
                }

                return View("Group", group);
            }
            return View("HomeNotFound");
        }

        [Route("home/{id}/id")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> HomeById(int id)
        {
            var habboHomeUser = await _userService.GetUserById(id.ToString());
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

        [Route("groups/edit/{homeId}")]
        [LoggedInFilter]
        public async Task<IActionResult> GroupEdit(int homeId)
        {
            var currentUser = await _userService.GetUserById(User.Identity.Name);
            var home = await _homeService.GetHomeDetailsById(homeId);
            if (home.UserId == currentUser.Id)
            {
                // Set editing mode
                CookieOptions option = new CookieOptions
                {
                    Expires = DateTime.Now.AddMinutes(30)
                };
                Response.Cookies.Append("editgroup", "true", option);

                return Redirect("/groups/" + home.Id + "/id");
            }

            return Redirect("/groups");
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
