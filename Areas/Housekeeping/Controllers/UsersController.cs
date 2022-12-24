using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Models;
using KeplerCMS.Data.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly IFriendService _friendsService;
        private readonly IFuseService _fuseService;
        public UsersController(IUserService userService, IFriendService friendsService, IFuseService fuseService)
        {
            _userService = userService;
            _friendsService = friendsService;
            _fuseService = fuseService;
        }

        [HousekeepingFilter(Fuse.fuse_kick)]
        public async Task<IActionResult> Index(string search = null, int currentPage = 1, string letter = null)
        {
            const int pageSize = 25;
            var take = pageSize;
            var skip = (currentPage - 1) * pageSize;
            var searchResult = await _userService.SearchUsers(search, take, skip, letter);
            var model = new UsersViewModel
            {
                Users = searchResult.Users,
                Search = search,
                Letter = letter,
                CurrentPage = currentPage,
                TotalPages = searchResult.TotalResults / pageSize
            };
            return View(model);
        }

        [HousekeepingFilter(Fuse.fuse_kick)]
        public async Task<IActionResult> Manage(int id, string message)
        {
            var user = await _userService.GetUserById(id);
            if (user == null) return NotFound();
            var friends = await _friendsService.GetFriendsWithIdAndUsername(user.Id);
            var rank = await _fuseService.GetRankById(user.Rank);
            var ranks = await _fuseService.GetRanks();
            var badges = await _userService.GetBadges(user.Id);
            var model = new ManageUserViewModel
            {
                User = user,
                Friends = friends,
                Rank = rank,
                Ranks = ranks,
                Message = message,
                Badges = badges
            };
            return View(model);
        }

        [HousekeepingFilter(Fuse.fuse_badges)]
        [HttpPost]
        public async Task<IActionResult> AddBadge([FromBody]UsersBadges badge)
        {
           var user = _userService.GetUserById(badge.UserId);
            if (user != null)
            {
                var currentBadges = await _userService.GetBadges(badge.UserId);
                if(currentBadges.Select(s=>s.Badge).Contains(badge.Badge)) {
                    return Ok("Badge already exists");
                } else {
                    await _userService.AddBadge(badge);
                }
            } else {
                return NotFound();
            }
            return Ok("Badges for user updated");
        }
        [HousekeepingFilter(Fuse.fuse_badges)]
        [HttpPost]
        public async Task<IActionResult> RemoveBadge([FromBody]UsersBadges badge)
        {
            var user = _userService.GetUserById(badge.UserId);
            if (user != null)
            {
               await _userService.RemoveBadge(badge);
            } else {
                return NotFound();
            }
            return Ok("Badges for user updated");
        }
    }

}

