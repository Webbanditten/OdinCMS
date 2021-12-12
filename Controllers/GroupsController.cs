using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using KeplerCMS.Models;
using Westwind.Globalization;
using KeplerCMS.Areas.MyHabbo.Models;
using System.Linq;

namespace KeplerCMS.Controllers
{
    [MaintenanceFilter]
    [MenuFilter]
    public class GroupsController : Controller
    {
        private readonly IUserService _userService;
        private readonly IHomeService _homeService;
        private readonly IRoomService _roomService;
        private readonly ISettingsService _settingsService;

        public GroupsController(IUserService userService, IHomeService homeService, IRoomService roomService, ISettingsService settingsService)
        {
            _userService = userService;
            _homeService = homeService;
            _roomService = roomService;
            _settingsService = settingsService;
        }


        [Route("groups/directory/recent")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> DirectoryRecent()
        {
            var recent = await _homeService.GetRecentGroups();
            
            var directory = new GroupDirectory { Recent = recent};
            
            return View(directory);
        }
        [Route("groups/directory/hotel")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> DirectoryHotel()
        {
            var hotel = await _homeService.GetHotelGroups();
            
            var directory = new GroupDirectory { Hotel = hotel };

            return View(directory);
        }
        [Route("groups/directory/active")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> DirectoryActive()
        {
            var active = await _homeService.GetActiveGroups();
            
            var directory = new GroupDirectory { Active = active };

            return View(directory);
        }

        [Route("groups/directory")]
        [LoggedInFilter(false)]
        public async Task<IActionResult> Directory()
        {
            var recent = await _homeService.GetRecentGroups();
            var active = await _homeService.GetActiveGroups();
            var hotel = await _homeService.GetHotelGroups();
            
            var directory = new GroupDirectory { Active = active, Recent = recent, Hotel = hotel };

            if(User.Identity.IsAuthenticated) { 
                directory.MyGroups = await _homeService.GetGroupsForUser(int.Parse(User.Identity.Name)); 
            }

            return View(directory);
        }

        [Route("myhabbo/groups/batch/confirm_accept")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> ConfirmAccept(int groupId, int[] targetIds)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if(await _homeService.CanEditHome(groupId, currentUserId))
            {
                return View();
            }
            return Content("Nope");
        }

        [Route("myhabbo/groups/batch/accept")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> Accept(int groupId, int[] targetIds)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if(await _homeService.CanEditHome(groupId, currentUserId))
            {
                await _homeService.AcceptMembers(groupId, targetIds);
                return Content("OK");
            }
            return Content("Nope");
        }

        [Route("myhabbo/groups/batch/confirm_decline")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> ConfirmDecline(int groupId, int[] targetIds)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if(await _homeService.CanEditHome(groupId, currentUserId))
            {
                return View();
            }
            return Content("Nope");
        }

        [Route("myhabbo/groups/batch/decline")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> Decline(int groupId, int[] targetIds)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if(await _homeService.CanEditHome(groupId, currentUserId))
            {
                await _homeService.RemoveMembers(groupId, targetIds);
                return Content("OK");
            }
            return Content("Nope");
        }

        [Route("myhabbo/groups/batch/confirm_give_rights")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> ConfirmGiveRights(int groupId, int[] targetIds)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if(await _homeService.CanEditHome(groupId, currentUserId))
            {
                return View();
            }
            return Content("Nope");
        }

        [Route("myhabbo/groups/batch/give_rights")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> GiveRights(int groupId, int[] targetIds)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if(await _homeService.CanEditHome(groupId, currentUserId))
            {
                await _homeService.GiveGroupRights(groupId, targetIds);
                return Content("OK");
            }
            return Content("Nope");
        }


        [Route("myhabbo/groups/batch/confirm_revoke_rights")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> ConfirmRevokeRights(int groupId, int[] targetIds)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if(await _homeService.CanEditHome(groupId, currentUserId))
            {
                return View();
            }
            return Content("Nope");
        }

        [Route("myhabbo/groups/batch/revoke_rights")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> RevokeRights(int groupId, int[] targetIds)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if(await _homeService.CanEditHome(groupId, currentUserId))
            {
                await _homeService.RemoveGroupRights(groupId, targetIds);
                return Content("OK");
            }
            return Content("Nope");
        }


        [Route("myhabbo/groups/batch/confirm_remove")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> ConfirmRemove(int groupId, int[] targetIds)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if(await _homeService.CanEditHome(groupId, currentUserId))
            {
                return View();
            }
            return Content("Nope");
        }

        [Route("myhabbo/groups/batch/remove")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> Remove(int groupId, int[] targetIds)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if(await _homeService.CanEditHome(groupId, currentUserId))
            {
                await _homeService.RemoveMembers(groupId, targetIds);
                return Content("OK");
            }
            return Content("Nope");
        }

        [Route("myhabbo/groups/memberlist")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> Memberlist(int pageNumber, int groupId, string searchString, bool pending)
        {
            var currentUserId = int.Parse(User.Identity.Name);
            if(await _homeService.CanEditHome(groupId, currentUserId))
            {
                var members = await _homeService.GetGroupMembers(groupId);
                var filtered_members = members.Where(s=>s.Pending == pending).ToList();
                Response.Headers.Add("x-json", "{\"pending\":\"" + DbRes.T("group_pending_members", "habbohome") + " (" + members.Count(s=>s.Pending == true) + ")\",\"members\":\"" + DbRes.T("group_members", "habbohome") + " (" + members.Count(s=>s.Pending == false) + ")\"}");
                return View(new Memberlist { Search = searchString, PageNumber = pageNumber, Members = filtered_members});
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
                return View(new GroupBadgeViewModel { Homes = home, Settings = await _settingsService.GetAll() });
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
        public IActionResult ConfirmLeave(int groupId)
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
            var currentUserId = int.Parse(User.Identity.Name);
            var membership = await _homeService.GetMembership(groupId, currentUserId);
            await _homeService.RemoveMembers(groupId, new int[] { membership.Id });
            return View();
        }

        [Route("groups/actions/join")]
        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> Join(int groupId)
        {
             var currentUserId = int.Parse(User.Identity.Name);
            var group = await _homeService.GetHomeDetailsById(groupId);
            var membership = await _homeService.AddPendingMember(groupId, currentUserId);
            if(group.GroupType == 1) {
                return View("JoinRequest");
            }
            await _homeService.AcceptMembers(groupId, new int[] { membership.Id });
            return View("Join");
        }

        
                
        [Route("groups/actions/confirm_deselect_favorite")]
        [LoggedInFilter]
        [HttpPost]
        public IActionResult ConfirmDeselectFavorite(int groupId)
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
