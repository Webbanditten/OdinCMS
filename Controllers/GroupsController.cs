using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using KeplerCMS.Models;
using Westwind.Globalization;

namespace KeplerCMS.Controllers
{
    [MenuFilter]
    public class GroupsController : Controller
    {
        private readonly IUserService _userService;
        private readonly IHomeService _homeService;
        private readonly IRoomService _roomService;
        public GroupsController(IUserService userService, IHomeService homeService, IRoomService roomService)
        {
            _userService = userService;
            _homeService = homeService;
            _roomService = roomService;
        }

        [Route("myhabbo/groups/memberlist")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> Memberlist(int pageNumber, int groupId, string searchString)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            var home = await _homeService.GetHomeDetailsById(groupId);
            if(home != null && await _homeService.CanEditHome(home.Id, currentUserId))
            {
                return View();
            }
            return Content("Nope");
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


        [Route("groups/actions/group_settings")]
        [LoggedInFilter]
        public async Task<IActionResult> GroupSettings(int groupId)
        {
            var currentUser = await _userService.GetUserById(int.Parse(User.Identity.Name));
            var home = await _homeService.GetHomeDetailsById(groupId);
            if(home != null && await _homeService.CanEditHome(home.Id, currentUser.Id))
            {
                return View(new GroupSettingsViewModel { Details = home, Rooms = await _roomService.GetRoomsByOwner(home.UserId)});
            }
            return Content("Nope");
        }

        [Route("groups/actions/check_group_url")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> CheckGroupUrl(string url, int groupId)
        {
            var currentUser = await _userService.GetUserById(int.Parse(User.Identity.Name));
            var home = await _homeService.GetHomeDetailsById(groupId);
            if(home != null && await _homeService.CanEditHome(home.Id, currentUser.Id))
            {
                // Check if other groups has the same URL then show message
                if(await _homeService.IsGroupUrlValid(url)) {
                    return Content(DbRes.T("url_success", "habbohome").Replace("{url}", url));
                }
            }
            return Content(DbRes.T("url_taken", "habbohome"));
        }

        [Route("groups/actions/update_group_settings")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> UpdateGroupSettings(string url, string name, string description, int type, int groupId, int? roomId)
        {
            var currentUser = await _userService.GetUserById(int.Parse(User.Identity.Name));
            var home = await _homeService.GetHomeDetailsById(groupId);
            if(home != null && await _homeService.CanEditHome(home.Id, currentUser.Id))
            {
                var updatedGroup = await _homeService.UpdateGroup(home.Id, name, description, url, type, roomId);
                return View(updatedGroup);
            }
            return Content("Something went wrong");
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

        [Route("groups/actions/confirm_delete_group")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> ConfirmDeleteGroup(int groupId)
        {
            // Redirect to group page
            var currentUser = await _userService.GetUserById(int.Parse(User.Identity.Name));
            var home = await _homeService.GetHomeDetailsById(groupId);
            if (home != null && currentUser != null && await _homeService.CanEditHome(home.Id, currentUser.Id))
            {
                return View(home);
            }
            return Content("Nope");

        }


        [Route("groups/actions/confirm_leave")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> ConfirmLeave(int groupId)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if (_homeService.GetMembership(groupId, currentUserId) != null)
            {
                return View();
            }
            return Content("Nope");
        }

        [Route("groups/actions/leave")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> Leave(int groupId)
        {
            return Content("Nope");
        }

        [Route("groups/actions/join")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> Join(int groupId)
        {
            return Content("Nope");
        }

        
                
        [Route("groups/actions/confirm_deselect_favorite")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> ConfirmDeselectFavorite(int groupId)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if (_homeService.GetMembership(groupId, currentUserId) != null) {
                return View();
            }
            return Content("Nope");
        }

        [Route("groups/actions/deselect_favorite")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> DeselectFavorite(int groupId)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if (_homeService.GetMembership(groupId, currentUserId) != null)
            {
                await _userService.SetGroup(currentUserId, 0);
            }
            return Content("OK");
        }


        [Route("groups/actions/select_favorite")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> SelectFavorite(int groupId)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if (_homeService.GetMembership(groupId, currentUserId) != null)
            {
                await _userService.SetGroup(currentUserId, groupId);
            }
            return Content("OK");
        }

        [Route("groups/actions/confirm_select_favorite")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> ConfirmSelectFavorite(int groupId)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if (_homeService.GetMembership(groupId, currentUserId) != null)
            {
                await _userService.SetGroup(currentUserId, groupId);
                return View();
            }
            return Content("Nope");
        }


        [Route("groups/actions/delete_group")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            // Redirect to group page
            var currentUserId = int.Parse(User.Identity.Name);
            var home = await _homeService.GetHomeDetailsById(groupId);
            if (home != null && home.UserId == currentUserId)
            {
                await _homeService.DeleteGroup(groupId);
                return View();
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
