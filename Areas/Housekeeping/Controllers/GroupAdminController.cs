using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    /// Hobba tools > Content moderation > Group editor (Sulake's "User groups > Group editor"): find a group by name, owner or
    /// id; change its name, description, web address, type and room; delete it (HomeService.DeleteGroup: members leave, notes
    /// and stickers go back to who placed them).
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.fuse_receive_calls_for_help)]
    public class GroupAdminController : Controller
    {
        public static readonly string[] GroupTypes = { "Regular", "Exclusive", "Closed" };

        private readonly DataContext _context;
        private readonly IHomeService _homes;
        private readonly IAuditLogService _audit;

        public GroupAdminController(DataContext context, IHomeService homes, IAuditLogService audit)
        {
            _context = context;
            _homes = homes;
            _audit = audit;
        }

        public record GroupRow(int Id, string Name, string Badge, string Url, int Type, int OwnerId, string Owner, int Members, DateTime Created);

        public class GroupsPage
        {
            public string Search { get; set; }
            public List<GroupRow> Groups { get; set; }
            public int Total { get; set; }
            public string Message { get; set; }
        }

        public class GroupEdit
        {
            public Homes Group { get; set; }
            public string Owner { get; set; }
            public int Members { get; set; }
            public int Pending { get; set; }
            public List<(int Id, string Name)> Rooms { get; set; }
            public string Message { get; set; }
        }

        public async Task<IActionResult> Index(string search = null, string message = null)
        {
            var query = _context.Homes.AsNoTracking().Where(h => h.Type == "group");
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            if (search != null)
            {
                var ownerIds = await _context.Users.AsNoTracking().Where(u => u.Username.Contains(search)).Select(u => u.Id).Take(200).ToListAsync();
                int.TryParse(search, out var id);
                query = query.Where(h => h.Id == id || h.GroupName.Contains(search) || h.GroupUrl.Contains(search) || ownerIds.Contains(h.UserId));
            }
            var groups = await query.OrderByDescending(h => h.Id).Take(100).ToListAsync();
            var ids = groups.Select(g => g.Id).ToList();
            var owners = groups.Select(g => g.UserId).Distinct().ToList();
            var names = await _context.Users.AsNoTracking().Where(u => owners.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username);
            var members = await _context.GroupMembers.AsNoTracking().Where(m => ids.Contains(m.GroupId) && m.IPending == 0).GroupBy(m => m.GroupId)
                .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count);
            return View(new GroupsPage
            {
                Search = search,
                Message = message,
                Total = await _context.Homes.CountAsync(h => h.Type == "group"),
                Groups = groups.Select(g => new GroupRow(g.Id, g.GroupName, g.GroupBadge, g.GroupUrl, g.GroupType, g.UserId, names.GetValueOrDefault(g.UserId),
                    members.GetValueOrDefault(g.Id), g.Created)).ToList(),
            });
        }

        public async Task<IActionResult> Edit(int id, string message = null)
        {
            var group = await _context.Homes.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id && h.Type == "group");
            if (group == null) return NotFound();
            var rooms = await _context.Rooms.AsNoTracking().Where(r => r.OwnerId == group.UserId).OrderBy(r => r.Name).Select(r => new { r.Id, r.Name }).ToListAsync();
            return View(new GroupEdit
            {
                Group = group,
                Owner = await _context.Users.AsNoTracking().Where(u => u.Id == group.UserId).Select(u => u.Username).FirstOrDefaultAsync(),
                Members = await _context.GroupMembers.CountAsync(m => m.GroupId == id && m.IPending == 0),
                Pending = await _context.GroupMembers.CountAsync(m => m.GroupId == id && m.IPending == 1),
                Rooms = rooms.Select(r => (r.Id, r.Name)).ToList(),
                Message = message,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int id, string name, string description, string url, int type, int? roomId)
        {
            var group = await _context.Homes.FirstOrDefaultAsync(h => h.Id == id && h.Type == "group");
            if (group == null) return NotFound();

            name = (name ?? "").Trim();
            if (name.Length == 0) return RedirectToAction("Edit", new { id, message = "A group needs a name." });
            if (name.Length > 30) name = name[..30];
            description = (description ?? "").Trim();
            if (description.Length > 2048) description = description[..2048];
            url = (url ?? "").Trim().ToLowerInvariant();
            if (url.Length > 0 && !Regex.IsMatch(url, "^[a-z0-9_-]{1,30}$"))
                return RedirectToAction("Edit", new { id, message = "The web address can have a-z, 0-9, - and _ (up to 30)." });
            if (url.Length > 0 && url != group.GroupUrl && await _context.Homes.AnyAsync(h => h.GroupUrl == url && h.Id != id))
                return RedirectToAction("Edit", new { id, message = "Another group has that web address." });
            if (type < 0 || type >= GroupTypes.Length) type = group.GroupType;
            if (roomId != null && !await _context.Rooms.AnyAsync(r => r.Id == roomId && r.OwnerId == group.UserId)) roomId = null;

            var changes = new List<string>();
            if (name != group.GroupName) changes.Add("name \"" + group.GroupName + "\" -> \"" + name + "\"");
            if (description != (group.GroupDescription ?? "")) changes.Add("description");
            if (url != (group.GroupUrl ?? "")) changes.Add("web address \"" + group.GroupUrl + "\" -> \"" + url + "\"");
            if (type != group.GroupType) changes.Add("type " + GroupTypes[type]);
            if (roomId != group.GroupRoom) changes.Add("room " + (roomId?.ToString() ?? "none"));
            if (changes.Count == 0) return RedirectToAction("Edit", new { id, message = "Nothing changed." });

            group.GroupName = name;
            group.GroupDescription = description;
            group.GroupUrl = url.Length == 0 ? null : url;
            group.GroupType = type;
            group.GroupRoom = roomId;
            await _context.SaveChangesAsync();
            var detail = string.Join("; ", changes);
            await _audit.AddLog(AuditLogType.edit_group, int.Parse(User.Identity.Name), "Edited group #" + id, detail.Length > 250 ? detail[..250] : detail, group.UserId, id);
            return RedirectToAction("Edit", new { id, message = "Saved: " + string.Join(", ", changes.Select(c => c.Split(' ')[0] == "web" ? "web address" : c.Split(' ')[0])) + "." });
        }

        /// <summary>Lets everyone pick the badge again: the group shows the default badge.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetBadge(int id)
        {
            var changed = await _context.Homes.Where(h => h.Id == id && h.Type == "group").ExecuteUpdateAsync(s => s.SetProperty(h => h.GroupBadge, "b0503Xs09114s05013s05015"));
            if (changed == 0) return NotFound();
            await _audit.AddLog(AuditLogType.edit_group, int.Parse(User.Identity.Name), "Edited group #" + id, "badge reset", 0, id);
            return RedirectToAction("Edit", new { id, message = "Badge reset to the default." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var group = await _context.Homes.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id && h.Type == "group");
            if (group == null) return RedirectToAction("Index", new { message = "That group is already gone." });
            if (!await _homes.DeleteGroup(id)) return RedirectToAction("Edit", new { id, message = "The group could not be deleted." });
            await _audit.AddLog(AuditLogType.delete_group, int.Parse(User.Identity.Name), "Deleted group #" + id, group.GroupName, group.UserId, id);
            return RedirectToAction("Index", new { message = "Deleted the group \"" + group.GroupName + "\"." });
        }
    }
}
