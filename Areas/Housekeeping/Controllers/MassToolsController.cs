using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// Hobba tools > Supervisor Hobba tools, as Sulake's: Mass ban, Mass alert and Mass unban for a list of Habbo names (one
    /// per line, or separated by commas). Bans and alerts go through Odin like Remote banning and Remote alerting; an unban
    /// ends the Habbos' running bans now (also IP and computer bans made with them).
    /// </summary>
    [Area("Housekeeping")]
    public class MassToolsController : Controller
    {
        public const int MaxNames = 200;

        /// <summary>"Choose a common message" (what the banned or alerted Habbo sees, in Danish).</summary>
        public static readonly string[] CommonMessages =
        {
            "Stødende indhold på Habbo Home",
            "Chikane af andre Habbos",
            "Personlige oplysninger på Habbo Home",
            "Reklame for andre hoteller eller sider",
            "Upassende sprog",
            "Snyd eller scamming",
        };

        private readonly DataContext _context;
        private readonly IAuditLogService _audit;
        private readonly ICommandQueueService _commandQueue;

        public MassToolsController(DataContext context, IAuditLogService audit, ICommandQueueService commandQueue)
        {
            _context = context;
            _audit = audit;
            _commandQueue = commandQueue;
        }

        public class MassPage
        {
            public string Names { get; set; }
            public string Message { get; set; }
            public string ExtraInfo { get; set; }
            public int BanLength { get; set; }
            public bool BanMachine { get; set; }
            public bool BanIp { get; set; }
            public string Result { get; set; }
            public List<string> NotFound { get; set; } = new();
        }

        private int Me => int.Parse(User.Identity.Name);

        /// <summary>The names as typed, without doubles, and the users they are.</summary>
        private async Task<(List<(int Id, string Username)> Users, List<string> NotFound)> Resolve(string names)
        {
            var list = (names ?? "").Split(new[] { '\n', '\r', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim()).Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxNames).ToList();
            var found = await _context.Users.AsNoTracking().Where(u => list.Contains(u.Username)).Select(u => new { u.Id, u.Username }).ToListAsync();
            var users = found.Select(u => (u.Id, u.Username)).ToList();
            var notFound = list.Where(n => !found.Any(u => string.Equals(u.Username, n, StringComparison.OrdinalIgnoreCase))).ToList();
            return (users, notFound);
        }

        [HousekeepingFilter(Fuse.fuse_ban)]
        public IActionResult Ban() => View(new MassPage { BanMachine = true, BanIp = true, BanLength = ReportsController.BanLengths[0] });

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.fuse_ban)]
        public async Task<IActionResult> Ban(MassPage model)
        {
            model.Message = (model.Message ?? "").Trim();
            if (!ReportsController.BanLengths.Contains(model.BanLength)) model.BanLength = ReportsController.BanLengths[0];
            if (model.Message.Length == 0) { model.Result = "Write the message the Habbos see first."; return View(model); }

            var (users, notFound) = await Resolve(model.Names);
            model.NotFound = notFound;
            if (users.Count == 0) { model.Result = "No Habbos with those names."; return View(model); }

            foreach (var user in users)
            {
                // One command per Habbo (Odin before v1.34.0 banned only a list's last name).
                _commandQueue.QueueCommand(CommandQueueType.remote_ban, new CommandTemplate
                {
                    Users = new[] { user.Username },
                    Message = model.Message,
                    BanLength = model.BanLength,
                    ExtraInfo = string.IsNullOrWhiteSpace(model.ExtraInfo) ? "Mass ban" : model.ExtraInfo.Trim(),
                    BanIp = model.BanIp,
                    BanMachine = model.BanMachine,
                    UserId = Me,
                });
                await _audit.AddLog(AuditLogType.mass_ban, Me, user.Username, model.BanLength + " hours: " + Short(model.Message), user.Id);
            }
            model.Result = "Banned " + users.Count + (users.Count == 1 ? " Habbo" : " Habbos") + " for " + model.BanLength + " hours: " + string.Join(", ", users.Select(u => u.Username)) + ".";
            model.Names = null;
            return View(model);
        }

        [HousekeepingFilter(Fuse.room_alert)]
        public IActionResult Alert() => View(new MassPage());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.room_alert)]
        public async Task<IActionResult> Alert(MassPage model)
        {
            model.Message = (model.Message ?? "").Trim();
            if (model.Message.Length == 0) { model.Result = "Write the message first."; return View(model); }

            var (users, notFound) = await Resolve(model.Names);
            model.NotFound = notFound;
            if (users.Count == 0) { model.Result = "No Habbos with those names."; return View(model); }

            _commandQueue.QueueCommand(CommandQueueType.remote_alert, new CommandTemplate { Users = users.Select(u => u.Username).ToArray(), Message = model.Message, UserId = Me });
            await _audit.AddLog(AuditLogType.mass_alert, Me, users.Count + " Habbos: " + Short(string.Join(", ", users.Select(u => u.Username))), Short(model.Message));
            model.Result = "Alert sent to " + users.Count + (users.Count == 1 ? " Habbo" : " Habbos") + " (those in the hotel see it now).";
            return View(model);
        }

        [HousekeepingFilter(Fuse.fuse_ban)]
        public IActionResult Unban() => View(new MassPage());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.fuse_ban)]
        public async Task<IActionResult> Unban(MassPage model)
        {
            var (users, notFound) = await Resolve(model.Names);
            model.NotFound = notFound;
            if (users.Count == 0) { model.Result = "No Habbos with those names."; return View(model); }

            // As List of current bans' Unban: the ban ends now (the row stays as history).
            var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var ids = users.Select(u => u.Id).ToList();
            var bans = await _context.UsersBans.Where(b => ids.Contains(b.UserId) && b.BannedUntilTimestamp > now).ToListAsync();
            foreach (var ban in bans) ban.BannedUntilTimestamp = now;
            await _context.SaveChangesAsync();

            var unbanned = users.Where(u => bans.Any(b => b.UserId == u.Id)).ToList();
            foreach (var user in unbanned)
                await _audit.AddLog(AuditLogType.mass_unban, Me, user.Username, bans.Count(b => b.UserId == user.Id) + " ban(s) removed", user.Id);
            var notBanned = users.Where(u => !unbanned.Contains(u)).Select(u => u.Username).ToList();
            model.Result = (unbanned.Count == 0 ? "None of them was banned." : "Unbanned " + string.Join(", ", unbanned.Select(u => u.Username)) + ".")
                + (unbanned.Count > 0 && notBanned.Count > 0 ? " Not banned: " + string.Join(", ", notBanned) + "." : "");
            model.Names = null;
            return View(model);
        }

        private static string Short(string s) => s.Length > 240 ? s[..240] + "…" : s;
    }
}
