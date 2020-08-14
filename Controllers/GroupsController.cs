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
    public class GroupsController : Controller
    {
        private readonly IUserService _userService;
        private readonly IHomeService _homeService;
        private readonly IFriendService _friendService;
        public GroupsController(IUserService userService, IHomeService homeService, IFriendService friendService)
        {
            _userService = userService;
            _homeService = homeService;
            _friendService = friendService;
        }

        [Route("groups/actions/show_badge_editor")]
        [LoggedInFilter]
        public async Task<IActionResult> BadgeEditor(int groupId)
        {
            var currentUser = await _userService.GetUserById(int.Parse(User.Identity.Name));
            var home = await _homeService.GetHomeDetailsById(groupId);
            if(home != null && await _homeService.CanEditHome(home.Id, currentUser.Id))
            {
                return View(home);
            }
            return Content("Nope");
        }

        [Route("groups/actions/update_group_badge")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> UpdateBadge(int groupId, string code)
        {
            // Redirect to group page
            var currentUser = await _userService.GetUserById(int.Parse(User.Identity.Name));
            var home = await _homeService.GetHomeDetailsById(groupId);
            if (home != null && currentUser != null && await _homeService.CanEditHome(home.Id, currentUser.Id))
            {
                await _homeService.UpdateGroupBadge(home.Id, code, currentUser.Id);
                return Redirect("/groups/" + home.Id + "/id");
            }
            return Content("Nope");

        }

        [Route("groups/{groupname:minlength(1)}")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> Group(string groupname)
        {
            var enableEditing = false;
            if (Request.Cookies["editid"] != null)
            {
                enableEditing = true;
            }


            var group = (User.Identity.IsAuthenticated) ? 
            await _homeService.GetHomeByGroupName(groupname, enableEditing, int.Parse(User.Identity.Name)) : 
            await _homeService.GetHomeByGroupName(groupname, enableEditing);
            if (group != null)
            {
                if (!User.Identity.IsAuthenticated || (!await _homeService.CanEditHome(group.Home.Id, int.Parse(User.Identity.Name)) || (Request.Cookies["editid"] != null && group.Home.Id != int.Parse(Request.Cookies["editid"]))))
                {
                    Response.Cookies.Delete("editid");
                    group.IsEditing = false;
                }

                return View(group);
            }
            return View("~/Views/HabboHome/HomeNotFound.cshtml");

        }

        [Route("groups/{id}/id")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> GroupById(int id)
        {
            var enableEditing = false;
            if (Request.Cookies["editid"] != null)
            {
                enableEditing = true;
            }


            var group = (User.Identity.IsAuthenticated) ? 
            await _homeService.GetHomeByGroupId(id, enableEditing, int.Parse(User.Identity.Name)) : 
            await _homeService.GetHomeByGroupId(id, enableEditing);
            if (group != null)
            {
                if (!User.Identity.IsAuthenticated || (!await _homeService.CanEditHome(group.Home.Id, int.Parse(User.Identity.Name)) || (Request.Cookies["editid"] != null && group.Home.Id != int.Parse(Request.Cookies["editid"]))))
                {
                    Response.Cookies.Delete("editid");
                    group.IsEditing = false;
                }

                return View("Group", group);
            }
            return View("~/Views/HabboHome/HomeNotFound.cshtml");
        }


        [Route("groups/edit/{homeId}")]
        [LoggedInFilter]
        public async Task<IActionResult> GroupEdit(int homeId)
        {
            var currentUser = await _userService.GetUserById(int.Parse(User.Identity.Name));
            var home = await _homeService.GetHomeDetailsById(homeId);
            if (await _homeService.CanEditHome(home.Id, currentUser.Id))
            {
                // Set editing mode
                CookieOptions option = new CookieOptions
                {
                    Expires = DateTime.Now.AddMinutes(30)
                };
                Response.Cookies.Append("editid", homeId.ToString(), option);

                return Redirect("/groups/" + home.Id + "/id");
            }

            return Redirect("/groups");
        }

    }
}
