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
using Microsoft.EntityFrameworkCore;
using KeplerCMS.Models.Enums;
using KeplerCMS.Data;
using System;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly IFriendService _friendsService;
        private readonly IFuseService _fuseService;
        private readonly IAuditLogService _auditService;
        private readonly DataContext _context;
        public UsersController(IUserService userService, IFriendService friendsService, IFuseService fuseService, IAuditLogService auditService, DataContext context)
        {
            _userService = userService;
            _friendsService = friendsService;
            _fuseService = fuseService;
            _auditService = auditService;
            _context = context;
        }

        /// <summary>"Permanent" trade ban: until 2100.</summary>
        public const long PermanentTradeBan = 4102444800;

        /// <summary>
        /// Trade ban: the user can't trade until then (hours = 0 lifts it, -1 = permanent). Odin reads the column on every
        /// trade, so it applies at once; the banned user gets "trade_youarenotallowed", whoever asks them "trade_othernotallowed".
        /// </summary>
        [HousekeepingFilter(Fuse.fuse_ban)]
        [HttpPost]
        public async Task<IActionResult> TradeBan(int id, int hours, string reason)
        {
            var user = await _context.Users.AsNoTracking().Where(u => u.Id == id).Select(u => new { u.Id, u.Username }).FirstOrDefaultAsync();
            if (user == null) return NotFound();

            long until = hours == 0 ? 0 : hours < 0 ? PermanentTradeBan : DateTimeOffset.UtcNow.ToUnixTimeSeconds() + hours * 3600L;
            await _context.Users.Where(u => u.Id == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.TradeBannedUntil, until));

            reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            if (reason != null && reason.Length > 200) reason = reason[..200];
            var me = int.Parse(HttpContext.User.Identity.Name);
            if (until == 0)
            {
                await _auditService.AddLog(AuditLogType.trade_unban, me, "Lifted the trade ban of " + user.Username, reason, user.Id);
                return RedirectToAction("Manage", new { id, message = "Trade ban lifted." });
            }

            var text = until == PermanentTradeBan ? "permanently" : "until " + DateTimeOffset.FromUnixTimeSeconds(until).ToLocalTime().ToString("dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            await _auditService.AddLog(AuditLogType.trade_ban, me, "Trade ban for " + user.Username + " " + text, reason, user.Id);
            return RedirectToAction("Manage", new { id, message = "Trade ban set " + text + "." });
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
                TotalPages = (searchResult.TotalResults + pageSize - 1) / pageSize
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
            var otherAccounts = await _userService.GetOtherAccounts(user.Id);
            var lastMachineId = await _userService.GetLastMachineId(user.Id);
            var purchases = await _userService.GetPurchases(user.Id, 200);
            var model = new ManageUserViewModel
            {
                User = user,
                OtherAccounts = otherAccounts,
                LastMachineId = lastMachineId,
                Friends = friends,
                Rank = rank,
                Ranks = ranks,
                Message = message,
                Badges = badges,
                Purchases = purchases
            };
            return View(model);
        }

        [HousekeepingFilter(Fuse.fuse_badges)]
        [HttpPost]
        public async Task<IActionResult> AddBadge([FromBody]UsersBadges badge)
        {
            if(badge.Badge == null || string.IsNullOrEmpty(badge.Badge)) {
                return BadRequest("Badge cannot be empty");
            }
            var user = _userService.GetUserById(badge.UserId);
            if (user != null)
            {
                var currentBadges = await _userService.GetBadges(badge.UserId);
                if(currentBadges.Select(s=>s.Badge).Contains(badge.Badge)) {
                    return Ok("Badge already exists");
                } else {
                    await _auditService.AddLog(AuditLogType.give_badge, int.Parse(HttpContext.User.Identity.Name), "Added " + badge.Badge, null, badge.UserId, 0);
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
            if(badge.Badge == null || string.IsNullOrEmpty(badge.Badge)) {
                return BadRequest("Badge cannot be empty");
            }
            var user = _userService.GetUserById(badge.UserId);
            if (user != null)
            {
                await _auditService.AddLog(AuditLogType.remove_badge, int.Parse(HttpContext.User.Identity.Name), "Removed " + badge.Badge, null, badge.UserId, 0);
                await _userService.RemoveBadge(badge);
            } else {
                return NotFound();
            }
            return Ok("Badges for user updated");
        }
    }

}

