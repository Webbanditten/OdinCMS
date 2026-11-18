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
    /// Admin tools > Room categories: the navigator's guest room categories (the Hotelværelser tab, "Gæsterum" = 4) as a
    /// list that can be rearranged (Views/RoomCategories/Index, wwwroot/housekeeping/js/room-categories.js). The best rated
    /// lists (rooms_categories.ranking, Odin's RoomRatings) always come first; the room categories are ordered, renamed,
    /// shown or hidden (is_hidden), opened to everyone's rooms or to staff only (fuse_setflatcat), allowed trading or not
    /// (allow_trading), added and deleted (their rooms move to a category picked on the page). Save writes everything in one
    /// transaction and sends reload_navigator (Odin v1.33.0+), which also moves loaded rooms and tells their visitors when
    /// trading changed.
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.fuse_administrator_access)]
    public class RoomCategoriesController : Controller
    {
        /// <summary>"Gæsterum": the client's navigator.private.default and navigator.visible.private.root.</summary>
        public const int RootCategory = 4;
        /// <summary>Odin's "no category" (SETFLATCAT's fallback; new rooms start there).</summary>
        public const int NoCategory = 2;
        public const string BusiestFirstSetting = "navigator.private.busiest.first";
        /// <summary>The fuse that lets staff put rooms in a staff-only category (Habbo Staff has it).</summary>
        public const string StaffFuse = "fuse_room_cat_special";
        /// <summary>Nobody has it: no room can be put in a best rated list.</summary>
        public const string RankingFuse = "fuse_ranking_category";
        public static readonly string[] Rankings = { "daily", "weekly", "alltime" };

        private readonly DataContext _context;
        private readonly ICommandQueueService _commandQueue;
        private readonly IAuditLogService _audit;

        public RoomCategoriesController(DataContext context, ICommandQueueService commandQueue, IAuditLogService audit)
        {
            _context = context;
            _commandQueue = commandQueue;
            _audit = audit;
        }

        public class CategoryView
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Hidden { get; set; }
            public string Ranking { get; set; }
            public bool Trading { get; set; }
            /// <summary>Any player may put their room here (fuse_setflatcat DEFAULT); otherwise only staff.</summary>
            public bool Everyone { get; set; }
            /// <summary>The fuse a player needs to see the category, when it isn't everyone's.</summary>
            public string Fuse { get; set; }
            public int Rooms { get; set; }
            public int VisitorsNow { get; set; }
        }

        public class PageView
        {
            public CategoryView Root { get; set; }
            public List<CategoryView> Lists { get; set; }
            public List<CategoryView> Categories { get; set; }
            /// <summary>Guest room categories the navigator doesn't open (Ingen kategori): shown for their rooms only.</summary>
            public List<CategoryView> Outside { get; set; }
            public bool BusiestFirst { get; set; }
        }

        private static bool IsDefault(string fuse) => string.IsNullOrEmpty(fuse) || string.Equals(fuse, "DEFAULT", StringComparison.OrdinalIgnoreCase);

        public async Task<IActionResult> Index()
        {
            var categories = await _context.NavigatorCategories.AsNoTracking().Where(c => c.PublicSpaces == 0).ToListAsync();
            var root = categories.FirstOrDefault(c => c.Id == RootCategory);
            if (root == null) return Content("The navigator has no category " + RootCategory + " (Gæsterum).");

            var counts = await _context.Rooms.AsNoTracking().Where(r => r.OwnerId != 0).GroupBy(r => r.Category)
                .Select(g => new { Category = g.Key, Rooms = g.Count(), Now = g.Sum(r => r.VisitorsNow) }).ToListAsync();
            CategoryView ViewOf(NavigatorCategory c)
            {
                var count = counts.FirstOrDefault(x => x.Category == c.Id);
                return new CategoryView
                {
                    Id = c.Id,
                    Name = c.Name,
                    Hidden = c.IsHidden,
                    Ranking = string.IsNullOrEmpty(c.Ranking) ? null : c.Ranking,
                    Trading = c.AllowTrading == 1,
                    Everyone = IsDefault(c.FuseSetFlatCat),
                    Fuse = IsDefault(c.FuseAccess) ? null : c.FuseAccess,
                    Rooms = count?.Rooms ?? 0,
                    VisitorsNow = count?.Now ?? 0,
                };
            }

            var children = categories.Where(c => c.ParentId == RootCategory && c.Id != RootCategory).OrderBy(c => c.OrderId).ThenBy(c => c.Id).ToList();
            var setting = await _context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Setting == BusiestFirstSetting);
            return View(new PageView
            {
                Root = ViewOf(root),
                Lists = children.Where(c => !string.IsNullOrEmpty(c.Ranking)).Select(ViewOf).ToList(),
                Categories = children.Where(c => string.IsNullOrEmpty(c.Ranking)).Select(ViewOf).ToList(),
                Outside = categories.Where(c => c.ParentId != RootCategory && c.Id != RootCategory).OrderBy(c => c.Id).Select(ViewOf).ToList(),
                BusiestFirst = setting == null || !string.Equals(setting.Value?.Trim(), "false", StringComparison.OrdinalIgnoreCase),
            });
        }

        public class CategoryEdit
        {
            /// <summary>The category's id, or "new..." for one made on the page.</summary>
            public string Key { get; set; }
            public string Name { get; set; }
            public bool Hidden { get; set; }
            /// <summary>For a new best rated list: daily, weekly or alltime.</summary>
            public string Ranking { get; set; }
            public bool Trading { get; set; }
            public bool Everyone { get; set; }
        }

        public class Deletion
        {
            public int Id { get; set; }
            /// <summary>Where its rooms go (a category kept on the page, or Ingen kategori).</summary>
            public int MoveTo { get; set; }
        }

        public class SaveRequest
        {
            public bool BusiestFirst { get; set; }
            public string RootName { get; set; }
            /// <summary>The best rated lists, in their order.</summary>
            public List<CategoryEdit> Lists { get; set; }
            /// <summary>The room categories, in their order.</summary>
            public List<CategoryEdit> Categories { get; set; }
            public List<Deletion> Deleted { get; set; }
        }

        private static string CleanName(string name)
        {
            name = Regex.Replace(name ?? "", @"[\p{Cc}]+", " ").Trim();
            return Regex.Replace(name, @"\s{2,}", " ");
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveRequest request)
        {
            if (request?.Lists == null || request.Categories == null) return BadRequest("Nothing to save.");
            var deletions = request.Deleted ?? new List<Deletion>();

            var categories = await _context.NavigatorCategories.Where(c => c.PublicSpaces == 0).ToListAsync();
            var root = categories.FirstOrDefault(c => c.Id == RootCategory);
            if (root == null) return BadRequest("The navigator has no category " + RootCategory + ".");
            var children = categories.Where(c => c.ParentId == RootCategory && c.Id != RootCategory).ToDictionary(c => c.Id);

            var rootName = CleanName(request.RootName);
            if (rootName.Length == 0 || rootName.Length > 100) return BadRequest("Give the guest rooms' list a name (at most 100 characters).");
            var all = request.Lists.Select(e => (Edit: e, IsList: true)).Concat(request.Categories.Select(e => (Edit: e, IsList: false))).ToList();
            var names = new Dictionary<string, string>();
            foreach (var (edit, isList) in all)
            {
                var name = CleanName(edit?.Name);
                if (name.Length == 0 || name.Length > 100) return BadRequest("Give every category a name (at most 100 characters).");
                if (edit.Key == null || names.ContainsKey(edit.Key)) return BadRequest("A category is on the page twice: reload the page.");
                names[edit.Key] = name;
                if (edit.Key.StartsWith("new"))
                {
                    if (isList && !Rankings.Contains(edit.Ranking)) return BadRequest("A new best rated list needs a kind (daily, weekly or all time).");
                    continue;
                }
                if (!int.TryParse(edit.Key, out var id) || !children.TryGetValue(id, out var existing)) return BadRequest("The categories changed since the page was opened: reload it.");
                if (string.IsNullOrEmpty(existing.Ranking) == isList) return BadRequest(existing.Name + " is in the wrong list: reload the page.");
            }

            var kept = all.Where(e => !e.Edit.Key.StartsWith("new")).Select(e => int.Parse(e.Edit.Key)).ToList();
            var deletedIds = deletions.Select(d => d.Id).Distinct().ToList();
            if (deletedIds.Any(id => !children.ContainsKey(id) || kept.Contains(id)) || children.Keys.Any(id => !kept.Contains(id) && !deletedIds.Contains(id)))
                return BadRequest("The categories changed since the page was opened: reload it.");

            // Where the rooms of a deleted category go: a kept room category, or a category outside Gæsterum (Ingen kategori).
            var targets = request.Categories.Where(e => !e.Key.StartsWith("new")).Select(e => int.Parse(e.Key))
                .Concat(categories.Where(c => c.ParentId != RootCategory && c.Id != RootCategory).Select(c => c.Id)).ToHashSet();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var created = new Dictionary<string, NavigatorCategory>();
            foreach (var (edit, isList) in all.Where(e => e.Edit.Key.StartsWith("new")))
            {
                var category = new NavigatorCategory
                {
                    ParentId = RootCategory,
                    IsNode = 0,
                    Name = names[edit.Key],
                    PublicSpaces = 0,
                    AllowTrading = 0,
                    MinroleAccess = 1,
                    MinroleSetFlatCat = isList ? 6 : 1,
                    FuseAccess = "DEFAULT",
                    FuseSetFlatCat = isList ? RankingFuse : "DEFAULT",
                    Ranking = isList ? edit.Ranking : "",
                };
                _context.NavigatorCategories.Add(category);
                created[edit.Key] = category;
            }

            root.Name = rootName;
            void Apply(List<CategoryEdit> edits, bool isList)
            {
                for (var i = 0; i < edits.Count; i++)
                {
                    var edit = edits[i];
                    var category = edit.Key.StartsWith("new") ? created[edit.Key] : children[int.Parse(edit.Key)];
                    category.Name = names[edit.Key];
                    category.OrderId = i + 1;
                    category.IsHidden = edit.Hidden;
                    if (isList) continue;
                    category.AllowTrading = edit.Trading ? 1 : 0;
                    // Staff only keeps a category's own staff fuse (Habbo Staff's fuse_room_cat_special).
                    if (edit.Everyone) category.FuseSetFlatCat = "DEFAULT";
                    else if (IsDefault(category.FuseSetFlatCat)) category.FuseSetFlatCat = StaffFuse;
                }
            }
            Apply(request.Lists, true);
            Apply(request.Categories, false);

            var moved = 0;
            foreach (var deletion in deletions)
            {
                // Checked here, in order: an earlier deletion may have moved rooms into this one. Returning rolls back.
                if (await _context.Rooms.AnyAsync(r => r.Category == deletion.Id))
                {
                    if (!targets.Contains(deletion.MoveTo)) return BadRequest("Pick where the rooms of " + children[deletion.Id].Name + " go.");
                    moved += await _context.Rooms.Where(r => r.Category == deletion.Id).ExecuteUpdateAsync(u => u.SetProperty(r => r.Category, deletion.MoveTo));
                }
                _context.NavigatorCategories.Remove(children[deletion.Id]);
            }

            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Setting == BusiestFirstSetting);
            var value = request.BusiestFirst ? "true" : "false";
            var settingChanged = setting == null || setting.Value?.Trim() != value;
            if (setting == null) _context.Settings.Add(new Settings { Setting = BusiestFirstSetting, Value = value });
            else setting.Value = value;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _audit.AddLog(AuditLogType.edit_room_categories, int.Parse(User.Identity.Name),
                "Room categories: " + request.Categories.Count + " categories, " + request.Lists.Count + " best rated lists, "
                + all.Count(e => e.Edit.Hidden) + " hidden",
                (created.Count > 0 ? "new: " + string.Join(", ", created.Values.Select(c => c.Name)) + ". " : "")
                + (deletions.Count > 0 ? "deleted: " + string.Join(", ", deletions.Select(d => children[d.Id].Name)) + " (" + moved + " rooms moved). " : "")
                + (request.BusiestFirst ? "busiest first" : "arranged order"));

            _commandQueue.QueueCommand(CommandQueueType.reload_navigator, new CommandTemplate());
            if (settingChanged) _commandQueue.QueueCommand(CommandQueueType.reload_settings, new CommandTemplate());
            return Json(new { ok = true });
        }
    }
}
