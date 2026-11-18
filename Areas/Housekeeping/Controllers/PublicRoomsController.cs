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
    /// Admin tools > Public rooms: the navigator's public room list as the client shows it (Views/PublicRooms/Index,
    /// wwwroot/housekeeping/js/public-rooms.js). Rooms are dragged into an order or another category and hidden or shown;
    /// the categories under "Fællesrum" are added, renamed, ordered and deleted. Save writes rooms.category,
    /// rooms.navigator_order, rooms.is_hidden and rooms_categories in one go and sends reload_navigator, so Odin (v1.31.0+)
    /// lists them that way at once; the client keeps a category it has seen for navigator.cache.duration (30 s).
    /// A hidden room joined to a shown one by a walkway (public_roomwalkways) is part of that room: Odin adds its visitors
    /// to the room's and forwards followers there, so parts aren't listed on their own and move with their room.
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.fuse_administrator_access)]
    public class PublicRoomsController : Controller
    {
        /// <summary>"Fællesrum": the client's navigator.public.default and navigator.visible.public.root.</summary>
        public const int RootCategory = 3;
        /// <summary>Odin's Room.isClubOnly: the rooms in this category are for Habbo Club members.</summary>
        public const int ClubCategory = 8;
        public const string BusiestFirstSetting = "navigator.public.busiest.first";

        private readonly DataContext _context;
        private readonly ICommandQueueService _commandQueue;
        private readonly IAuditLogService _audit;

        public PublicRoomsController(DataContext context, ICommandQueueService commandQueue, IAuditLogService audit)
        {
            _context = context;
            _commandQueue = commandQueue;
            _audit = audit;
        }

        public class PartView
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class RoomView
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Hidden { get; set; }
            public int VisitorsNow { get; set; }
            public int VisitorsMax { get; set; }
            public List<PartView> Parts { get; set; } = new();
            /// <summary>Why the navigator doesn't list it (rooms outside the navigator's categories only).</summary>
            public string Outside { get; set; }
        }

        public class CategoryView
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool ClubOnly { get; set; }
            /// <summary>The fuse a player needs to see the category, when it isn't everyone's (DEFAULT).</summary>
            public string Fuse { get; set; }
            /// <summary>Out of the navigator (rooms_categories.is_hidden, Odin v1.33.0).</summary>
            public bool Hidden { get; set; }
            public List<RoomView> Rooms { get; set; } = new();
        }

        public class PageView
        {
            public CategoryView Root { get; set; }
            public List<CategoryView> Categories { get; set; }
            public List<RoomView> Outside { get; set; }
            public bool BusiestFirst { get; set; }
        }

        public async Task<IActionResult> Index()
        {
            var rooms = await PublicRooms(false);
            var categories = await _context.NavigatorCategories.AsNoTracking().ToListAsync();
            var root = categories.FirstOrDefault(c => c.Id == RootCategory);
            if (root == null) return Content("The navigator has no category " + RootCategory + " (Fællesrum).");

            var owners = await Owners(rooms);
            var subs = categories.Where(IsSubCategory).OrderBy(c => c.OrderId).ThenBy(c => c.Id).ToList();
            var listed = subs.Select(c => c.Id).Append(RootCategory).ToHashSet();

            RoomView RoomOf(Rooms room) => new RoomView
            {
                Id = room.Id,
                Name = room.Name,
                Hidden = room.IsHidden,
                VisitorsNow = room.VisitorsNow + rooms.Where(p => p.Id != room.Id && owners[p.Id] == room.Id).Sum(p => p.VisitorsNow),
                VisitorsMax = room.VisitorsMax + rooms.Where(p => p.Id != room.Id && owners[p.Id] == room.Id).Sum(p => p.VisitorsMax),
                Parts = rooms.Where(p => p.Id != room.Id && owners[p.Id] == room.Id).OrderBy(p => p.Id).Select(p => new PartView { Id = p.Id, Name = p.Name }).ToList(),
            };
            var main = rooms.Where(r => owners[r.Id] == r.Id).OrderBy(r => r.NavigatorOrder).ThenBy(r => r.Id).ToList();
            CategoryView Category(NavigatorCategory c) => new CategoryView
            {
                Id = c.Id,
                Name = c.Name,
                ClubOnly = c.Id == ClubCategory,
                Fuse = string.Equals(c.FuseAccess, "DEFAULT", StringComparison.OrdinalIgnoreCase) ? null : c.FuseAccess,
                Hidden = c.IsHidden,
                Rooms = main.Where(r => r.Category == c.Id).Select(RoomOf).ToList(),
            };

            var outside = main.Where(r => !listed.Contains(r.Category)).OrderBy(r => r.Name).Select(r =>
            {
                var view = RoomOf(r);
                var category = categories.FirstOrDefault(c => c.Id == r.Category);
                view.Outside = category == null ? (r.Category == 0 ? "no category" : "category " + r.Category + " doesn't exist")
                    : category.PublicSpaces == 0 ? "in the guest room category " + category.Name
                    : "in " + category.Name + ", which the navigator doesn't open";
                return view;
            }).ToList();

            var setting = await _context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Setting == BusiestFirstSetting);
            return View(new PageView
            {
                Root = Category(root),
                Categories = subs.Select(Category).ToList(),
                Outside = outside,
                BusiestFirst = setting == null || !string.Equals(setting.Value?.Trim(), "false", StringComparison.OrdinalIgnoreCase),
            });
        }

        private static bool IsSubCategory(NavigatorCategory c) => c.ParentId == RootCategory && c.PublicSpaces == 1 && c.Id != RootCategory;

        private async Task<List<Rooms>> PublicRooms(bool tracked)
        {
            var query = _context.Rooms.Where(r => r.OwnerId == 0);
            return await (tracked ? query : query.AsNoTracking()).ToListAsync();
        }

        /// <summary>
        /// Which room each public room belongs to (itself for a room of its own). Hidden rooms joined by walkways to a shown
        /// room belong to the nearest shown one, as in Odin's RoomManager.getChildRooms; a group with no shown room belongs
        /// to its lowest id (hidden as a whole).
        /// </summary>
        private async Task<Dictionary<int, int>> Owners(List<Rooms> rooms)
        {
            var walkways = await _context.PublicRoomWalkways.AsNoTracking().ToListAsync();
            var hidden = rooms.ToDictionary(r => r.Id, r => r.IsHidden);
            var next = hidden.Keys.ToDictionary(id => id, _ => new SortedSet<int>());
            foreach (var w in walkways)
            {
                if (w.RoomId is not int a || w.ToId is not int b || a == b || !next.ContainsKey(a) || !next.ContainsKey(b)) continue;
                next[a].Add(b);
                next[b].Add(a);
            }

            var owner = new Dictionary<int, int>();
            void Spread(Queue<int> queue)
            {
                while (queue.Count > 0)
                {
                    var room = queue.Dequeue();
                    foreach (var other in next[room])
                    {
                        if (owner.ContainsKey(other) || !hidden[other]) continue;
                        owner[other] = owner[room];
                        queue.Enqueue(other);
                    }
                }
            }

            var shown = new Queue<int>();
            foreach (var id in hidden.Keys.OrderBy(id => id).Where(id => !hidden[id]))
            {
                owner[id] = id;
                shown.Enqueue(id);
            }
            Spread(shown);
            foreach (var id in hidden.Keys.OrderBy(id => id).Where(id => !owner.ContainsKey(id)))
            {
                owner[id] = id;
                Spread(new Queue<int>(new[] { id }));
            }
            return owner;
        }

        public class CategoryEdit
        {
            /// <summary>The category's id, or "new..." for one made on the page.</summary>
            public string Key { get; set; }
            public string Name { get; set; }
            public bool Hidden { get; set; }
        }

        public class RoomEdit
        {
            public int Id { get; set; }
            public string Category { get; set; }
            public bool Hidden { get; set; }
        }

        public class SaveRequest
        {
            public bool BusiestFirst { get; set; }
            public string RootName { get; set; }
            /// <summary>The categories under Fællesrum, in their order.</summary>
            public List<CategoryEdit> Categories { get; set; }
            public List<int> Deleted { get; set; }
            /// <summary>Every room listed on the page's categories, in its category's order.</summary>
            public List<RoomEdit> Rooms { get; set; }
        }

        private static string CleanName(string name)
        {
            name = Regex.Replace(name ?? "", @"[\p{Cc}]+", " ").Trim();
            return Regex.Replace(name, @"\s{2,}", " ");
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveRequest request)
        {
            if (request?.Categories == null || request.Rooms == null) return BadRequest("Nothing to save.");
            var deleted = (request.Deleted ?? new List<int>()).Distinct().ToList();

            var categories = await _context.NavigatorCategories.ToListAsync();
            var root = categories.FirstOrDefault(c => c.Id == RootCategory);
            if (root == null) return BadRequest("The navigator has no category " + RootCategory + ".");
            var subs = categories.Where(IsSubCategory).ToDictionary(c => c.Id);

            var rootName = CleanName(request.RootName);
            if (rootName.Length == 0 || rootName.Length > 100) return BadRequest("Give the public rooms' list a name (at most 100 characters).");
            var names = new Dictionary<string, string>();
            foreach (var edit in request.Categories)
            {
                var name = CleanName(edit?.Name);
                if (name.Length == 0 || name.Length > 100) return BadRequest("Give every category a name (at most 100 characters).");
                if (edit.Key == null || names.ContainsKey(edit.Key)) return BadRequest("A category is on the page twice: reload the page.");
                names[edit.Key] = name;
            }

            var kept = request.Categories.Where(c => !c.Key.StartsWith("new")).Select(c => int.TryParse(c.Key, out var id) ? id : -1).ToList();
            if (kept.Any(id => !subs.ContainsKey(id)) || deleted.Any(id => !subs.ContainsKey(id) || kept.Contains(id))
                || subs.Keys.Any(id => !kept.Contains(id) && !deleted.Contains(id)))
                return BadRequest("The categories changed since the page was opened: reload it.");
            if (deleted.Contains(ClubCategory)) return BadRequest(subs[ClubCategory].Name + " can't be deleted: the game makes the rooms in it members-only.");

            var rooms = await PublicRooms(true);
            var owners = await Owners(rooms);
            var byId = rooms.ToDictionary(r => r.Id);
            if (request.Rooms.Select(r => r.Id).Distinct().Count() != request.Rooms.Count) return BadRequest("A room is on the page twice: reload the page.");
            foreach (var edit in request.Rooms)
            {
                if (!byId.ContainsKey(edit.Id) || owners[edit.Id] != edit.Id) return BadRequest("Room " + edit.Id + " isn't a public room of its own: reload the page.");
                if (edit.Category != RootCategory.ToString() && !names.ContainsKey(edit.Category ?? ""))
                    return BadRequest(byId[edit.Id].Name + " is in a category that isn't on the page: reload it.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var ids = new Dictionary<string, int> { [RootCategory.ToString()] = RootCategory };
            var created = new List<NavigatorCategory>();
            foreach (var edit in request.Categories.Where(c => c.Key.StartsWith("new")))
            {
                var category = new NavigatorCategory
                {
                    ParentId = RootCategory,
                    IsNode = 0,
                    Name = names[edit.Key],
                    PublicSpaces = 1,
                    AllowTrading = root.AllowTrading,
                    MinroleAccess = root.MinroleAccess,
                    MinroleSetFlatCat = root.MinroleSetFlatCat,
                    FuseAccess = "DEFAULT",
                    FuseSetFlatCat = "DEFAULT",
                };
                _context.NavigatorCategories.Add(category);
                created.Add(category);
            }
            await _context.SaveChangesAsync();
            var newIndex = 0;
            foreach (var edit in request.Categories)
            {
                ids[edit.Key] = edit.Key.StartsWith("new") ? created[newIndex++].Id : int.Parse(edit.Key);
            }

            root.Name = rootName;
            for (var i = 0; i < request.Categories.Count; i++)
            {
                var category = categories.FirstOrDefault(c => c.Id == ids[request.Categories[i].Key]) ?? created.First(c => c.Id == ids[request.Categories[i].Key]);
                category.Name = names[request.Categories[i].Key];
                category.OrderId = i + 1;
                category.IsHidden = request.Categories[i].Hidden;
            }

            var places = new Dictionary<int, int>();
            foreach (var edit in request.Rooms)
            {
                var room = byId[edit.Id];
                var category = ids[edit.Category];
                places[category] = places.GetValueOrDefault(category) + 1;
                room.Category = category;
                room.NavigatorOrder = places[category];
                room.IsHidden = edit.Hidden;
            }
            // A room's walkway parts go where it goes (and stay hidden).
            foreach (var part in rooms.Where(r => owners[r.Id] != r.Id && request.Rooms.Any(e => e.Id == owners[r.Id])))
            {
                part.Category = byId[owners[part.Id]].Category;
            }

            if (deleted.Count > 0)
            {
                var inUse = rooms.Where(r => deleted.Contains(r.Category)).Select(r => r.Category)
                    .Concat(await _context.Rooms.Where(r => r.OwnerId != 0 && deleted.Contains(r.Category)).Select(r => r.Category).ToListAsync()).Distinct().ToList();
                if (inUse.Count > 0) return BadRequest("Move the rooms out of " + string.Join(", ", inUse.Select(id => subs[id].Name)) + " before deleting it.");
                _context.NavigatorCategories.RemoveRange(deleted.Select(id => subs[id]));
            }

            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Setting == BusiestFirstSetting);
            var value = request.BusiestFirst ? "true" : "false";
            var settingChanged = setting == null || setting.Value?.Trim() != value;
            if (setting == null) _context.Settings.Add(new Settings { Setting = BusiestFirstSetting, Value = value });
            else setting.Value = value;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var listed = request.Rooms.Count;
            var hiddenCount = request.Rooms.Count(r => r.Hidden);
            await _audit.AddLog(AuditLogType.edit_public_rooms, int.Parse(User.Identity.Name),
                "Public rooms: " + (request.Categories.Count + 1) + " categories, " + listed + " rooms (" + hiddenCount + " hidden)",
                (created.Count > 0 ? "new: " + string.Join(", ", created.Select(c => c.Name)) + ". " : "")
                + (deleted.Count > 0 ? "deleted: " + string.Join(", ", deleted.Select(id => subs[id].Name)) + ". " : "")
                + (request.BusiestFirst ? "busiest first" : "arranged order"));

            _commandQueue.QueueCommand(CommandQueueType.reload_navigator, new CommandTemplate());
            if (settingChanged) _commandQueue.QueueCommand(CommandQueueType.reload_settings, new CommandTemplate());
            return Json(new { ok = true });
        }
    }
}
