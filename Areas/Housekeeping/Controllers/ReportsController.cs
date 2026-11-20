using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// Hobba tools > Content moderation: one page per kind of report ("Stickie Note Reports", "Guestbook Reports", ...), laid
    /// out like Sulake's housekeeping: query (new reports, by writer, by reporter, archived), a results table with checkboxes,
    /// Toggle / Delete and archive / Archive, and "Ban selected..." (bans the checked rows' writers, then deletes and archives).
    /// A row is one reported thing; acting on it archives all its open reports. The reports come from ReportController.
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.fuse_receive_calls_for_help)]
    public class ReportsController : Controller
    {
        public static readonly int[] ShowMax = { 20, 50, 100, 200 };
        /// <summary>The ban lengths of Hobba tools > Remote banning, in hours.</summary>
        public static readonly int[] BanLengths = { 2, 4, 12, 24, 2 * 24, 3 * 24, 7 * 24, 14 * 24, 21 * 24, 30 * 24, 60 * 24, 365 * 24, 730 * 24, 100000 };

        private readonly DataContext _context;
        private readonly IAuditLogService _audit;
        private readonly ICommandQueueService _commandQueue;

        public ReportsController(DataContext context, IAuditLogService audit, ICommandQueueService commandQueue)
        {
            _context = context;
            _audit = audit;
            _commandQueue = commandQueue;
        }

        /// <summary>One reported thing (or, when archived, one handling of it): its reports newest first and what it says now.</summary>
        public class ReportRow
        {
            public int ObjectId { get; set; }
            public List<WebsiteReport> Reports { get; set; }
            /// <summary>The text now; null when it's gone.</summary>
            public string Current { get; set; }
            /// <summary>The page it was reported on.</summary>
            public string Link { get; set; }
            public WebsiteReport Latest => Reports[0];
            public bool IsOpen => Latest.Status == WebsiteReports.Open;
        }

        public class ReportsPage
        {
            public string Type { get; set; }
            public string Criteria { get; set; }
            public string Username { get; set; }
            public int Max { get; set; }
            public int Total { get; set; }
            public int New { get; set; }
            public List<ReportRow> Rows { get; set; }
            public Dictionary<int, string> Names { get; set; }
            public string Message { get; set; }
            public string Name(int? id) => id != null && Names.TryGetValue(id.Value, out var n) ? n : id?.ToString() ?? "-";
        }

        public async Task<IActionResult> Index(string type = WebsiteReports.Stickie, string criteria = "new", string username = null, int max = 20, string message = null)
        {
            if (!WebsiteReports.Types.ContainsKey(type ?? "")) type = WebsiteReports.Stickie;
            if (!ShowMax.Contains(max)) max = ShowMax[0];
            username = string.IsNullOrWhiteSpace(username) ? null : username.Trim();

            var query = _context.WebsiteReports.AsNoTracking().Where(r => r.Type == type);
            var page = new ReportsPage { Type = type, Criteria = criteria, Username = username, Max = max, Message = message };
            page.Total = await query.CountAsync();
            page.New = await query.CountAsync(r => r.Status == WebsiteReports.Open);

            int? userId = username == null ? null : await _context.Users.AsNoTracking().Where(u => u.Username == username).Select(u => (int?)u.Id).FirstOrDefaultAsync();
            switch (criteria)
            {
                case "writer":
                    query = query.Where(r => r.AuthorId == (userId ?? -1));
                    break;
                case "reporter":
                    query = query.Where(r => r.ReporterId == (userId ?? -1));
                    break;
                case "archived":
                    query = query.Where(r => r.Status == WebsiteReports.Closed);
                    break;
                default:
                    page.Criteria = "new";
                    query = query.Where(r => r.Status == WebsiteReports.Open);
                    break;
            }
            var reports = await query.OrderByDescending(r => r.Id).Take(max * 10).ToListAsync();

            // Open reports of one thing share a row; archived ones share a row per handling (closed together).
            page.Rows = reports
                .GroupBy(r => (r.ObjectId, r.Status, r.ClosedAt))
                .Select(g => new ReportRow { ObjectId = g.Key.ObjectId, Reports = g.ToList() })
                .OrderByDescending(g => g.IsOpen ? g.Latest.CreatedAt : g.Latest.ClosedAt ?? g.Latest.CreatedAt)
                .Take(max)
                .ToList();
            foreach (var row in page.Rows) row.Current = await CurrentText(type, row.ObjectId);
            await AddLinks(page.Rows);

            var userIds = page.Rows.SelectMany(g => g.Reports.SelectMany(r => new[] { (int?)r.ReporterId, r.AuthorId, r.ClosedBy }))
                .Where(id => id != null).Select(id => id.Value).Distinct().ToList();
            page.Names = await _context.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username);
            return View(page);
        }

        /// <summary>
        /// "Archive" (no action) or "Delete and archive" (remove = true: notes and entries deleted, mottos and group
        /// descriptions cleared, groups renamed to their row's new name) for the checked rows.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(string type, int[] ids, bool remove = false, string criteria = null, string username = null, int max = 20)
        {
            if (ids == null || ids.Length == 0) return Back(type, criteria, username, max, "Check at least one row.");
            var results = new List<string>();
            foreach (var id in ids.Distinct())
            {
                var outcome = remove ? await Remove(type, id, Request.Form["name_" + id]) : "No action";
                if (outcome.StartsWith("!")) { results.Add("#" + id + ": " + outcome.Substring(1)); continue; }
                var closed = await Close(type, id, outcome);
                if (closed > 0) results.Add("#" + id + ": " + char.ToLowerInvariant(outcome[0]) + outcome.Substring(1));
            }
            return Back(type, criteria, username, max, results.Count == 0 ? "Already handled." : string.Join("; ", results) + ".");
        }

        /// <summary>"Ban and archive": bans the writers of the checked rows (Odin's remote ban), then deletes and archives them.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.fuse_ban)]
        public async Task<IActionResult> BanAndArchive(string type, int[] ids, int banLength, string banMessage, string extraInfo, bool banMachine = false,
            bool banIp = false, string criteria = null, string username = null, int max = 20)
        {
            if (ids == null || ids.Length == 0) return Back(type, criteria, username, max, "Check at least one row.");
            banMessage = (banMessage ?? "").Trim();
            if (banMessage.Length == 0) return Back(type, criteria, username, max, "Write the message the Habbo sees first.");
            if (!BanLengths.Contains(banLength)) banLength = BanLengths[0];

            var staffId = int.Parse(User.Identity.Name);
            var authorIds = await _context.WebsiteReports.AsNoTracking()
                .Where(r => r.Type == type && ids.Contains(r.ObjectId) && r.Status == WebsiteReports.Open && r.AuthorId != null)
                .Select(r => r.AuthorId.Value).Distinct().ToListAsync();
            var authors = await _context.Users.AsNoTracking().Where(u => authorIds.Contains(u.Id)).Select(u => new { u.Id, u.Username }).ToListAsync();
            foreach (var author in authors)
            {
                // One command per Habbo: Odin's RemoteBanCommand bans the list's last name once per entry.
                _commandQueue.QueueCommand(CommandQueueType.remote_ban, new CommandTemplate
                {
                    Users = new[] { author.Username },
                    Message = banMessage,
                    BanLength = banLength,
                    ExtraInfo = string.IsNullOrWhiteSpace(extraInfo) ? "Website report: " + WebsiteReports.TypeLabel(type) : extraInfo.Trim(),
                    BanIp = banIp,
                    BanMachine = banMachine,
                    UserId = staffId,
                });
                await _audit.AddLog(AuditLogType.ban_user, staffId, author.Username, banLength + " hours, website report: " + WebsiteReports.TypeLabel(type), author.Id);
            }

            var removable = WebsiteReports.RemoveLabel(type) != null && type != WebsiteReports.GroupName;
            foreach (var id in ids.Distinct())
            {
                var outcome = removable ? await Remove(type, id, null) : null;
                if (outcome == null || outcome.StartsWith("!")) outcome = "Writer banned";
                else outcome += ", writer banned";
                await Close(type, id, outcome);
            }
            var names = string.Join(", ", authors.Select(a => a.Username));
            return Back(type, criteria, username, max, authors.Count == 0 ? "Nobody to ban; archived." : "Banned " + names + " for " + banLength + " hours and archived.");
        }

        private IActionResult Back(string type, string criteria, string username, int max, string message) =>
            RedirectToAction("Index", new { type, criteria, username, max, message });

        /// <summary>Archives every open report of one thing; returns how many.</summary>
        private async Task<int> Close(string type, int objectId, string outcome)
        {
            var open = await _context.WebsiteReports.Where(r => r.Type == type && r.ObjectId == objectId && r.Status == WebsiteReports.Open).ToListAsync();
            if (open.Count == 0) return 0;
            var staffId = int.Parse(User.Identity.Name);
            var now = DateTime.Now;
            foreach (var r in open)
            {
                r.Status = WebsiteReports.Closed;
                r.Outcome = outcome;
                r.ClosedBy = staffId;
                r.ClosedAt = now;
            }
            await _context.SaveChangesAsync();
            await _audit.AddLog(AuditLogType.website_report_close, staffId, "Website report: " + WebsiteReports.TypeLabel(type) + " #" + objectId,
                outcome + " (" + open.Count + (open.Count == 1 ? " report)" : " reports)"), open[0].AuthorId ?? 0, objectId);
            return open.Count;
        }

        /// <summary>Removes the reported text; returns the outcome, or "!reason" when nothing was done.</summary>
        private async Task<string> Remove(string type, int id, string newName)
        {
            switch (type)
            {
                case WebsiteReports.Stickie:
                {
                    var note = await _context.HomesItems.FirstOrDefaultAsync(i => i.Id == id);
                    if (note == null) return "Note already gone";
                    _context.HomesItems.Remove(note);
                    await _context.SaveChangesAsync();
                    return "Note deleted";
                }
                case WebsiteReports.Guestbook:
                {
                    var entry = await _context.HomesGuestbook.FirstOrDefaultAsync(g => g.Id == id);
                    if (entry == null) return "Entry already gone";
                    _context.HomesGuestbook.Remove(entry);
                    await _context.SaveChangesAsync();
                    return "Entry deleted";
                }
                case WebsiteReports.Motto:
                {
                    var changed = await _context.Users.Where(u => u.Id == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Motto, ""));
                    if (changed == 0) return "!that Habbo doesn't exist any more";
                    // As the profile page does: the hotel shows the new motto at once.
                    _commandQueue.QueueCommand(CommandQueueType.refresh_appearance, new CommandTemplate { UserId = id });
                    return "Motto cleared";
                }
                case WebsiteReports.GroupDesc:
                {
                    var changed = await _context.Homes.Where(h => h.Id == id && h.Type == "group").ExecuteUpdateAsync(s => s.SetProperty(h => h.GroupDescription, ""));
                    return changed == 0 ? "!that group doesn't exist any more" : "Description cleared";
                }
                case WebsiteReports.GroupName:
                {
                    newName = (newName ?? "").Trim();
                    if (newName.Length == 0) return "!write its new name in the row first";
                    if (newName.Length > 30) newName = newName.Substring(0, 30);
                    var changed = await _context.Homes.Where(h => h.Id == id && h.Type == "group").ExecuteUpdateAsync(s => s.SetProperty(h => h.GroupName, newName));
                    return changed == 0 ? "!that group doesn't exist any more" : "Renamed to \"" + newName + "\"";
                }
            }
            return "!can't be removed from here";
        }

        private async Task<string> CurrentText(string type, int id) => type switch
        {
            WebsiteReports.Name => await _context.Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.Username).FirstOrDefaultAsync(),
            WebsiteReports.Motto => await _context.Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.Motto ?? "").FirstOrDefaultAsync(),
            WebsiteReports.Stickie => await _context.HomesItems.AsNoTracking().Where(i => i.Id == id).Select(i => i.Data ?? "").FirstOrDefaultAsync(),
            WebsiteReports.Guestbook => await _context.HomesGuestbook.AsNoTracking().Where(g => g.Id == id).Select(g => g.Message ?? "").FirstOrDefaultAsync(),
            WebsiteReports.Room => await _context.Rooms.AsNoTracking().Where(r => r.Id == id).Select(r => r.Name + "\n" + (r.Description ?? "")).FirstOrDefaultAsync(),
            WebsiteReports.GroupName => await _context.Homes.AsNoTracking().Where(h => h.Id == id).Select(h => h.GroupName ?? "").FirstOrDefaultAsync(),
            WebsiteReports.GroupDesc => await _context.Homes.AsNoTracking().Where(h => h.Id == id).Select(h => h.GroupDescription ?? "").FirstOrDefaultAsync(),
            _ => null,
        };

        /// <summary>The page it was reported on: /home/USERID/id for a Habbo Home, /groups/HOMEID/id for a group.</summary>
        private async Task AddLinks(List<ReportRow> rows)
        {
            var homeIds = rows.Select(g => g.Latest.HomeId).Where(id => id != null).Select(id => id.Value).Distinct().ToList();
            var homes = await _context.Homes.AsNoTracking().Where(h => homeIds.Contains(h.Id)).ToDictionaryAsync(h => h.Id, h => new { h.Type, h.UserId });
            foreach (var row in rows)
            {
                if (row.Latest.HomeId is int homeId && homes.TryGetValue(homeId, out var home))
                    row.Link = home.Type == "group" ? "/groups/" + homeId + "/id" : "/home/" + home.UserId + "/id";
            }
        }
    }
}
