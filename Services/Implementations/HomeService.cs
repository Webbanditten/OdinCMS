using KeplerCMS.Areas.MyHabbo.Helpers;
using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Westwind.Globalization;

namespace KeplerCMS.Services.Implementations
{
    public class HomeService : IHomeService
    {
        private const string UserHome = "user";
        private const string GroupHome = "group";
        private const string Widgets = "widgets";
        private const string Notes = "notes";
        private const string Backgrounds = "backgrounds";
        private const string DefaultSkin = "w_skin_defaultskin";
        private const string DefaultBackground = "b_bg_pattern_abstract2";
        private const int MaxGuestbookLength = 500;
        private const int MaxNoteLength = 5000;

        // A home is created on the first view; two simultaneous first views used to create two homes.
        private static readonly SemaphoreSlim HomeCreation = new SemaphoreSlim(1, 1);

        private readonly DataContext _context;
        private readonly IUserService _userService;
        private readonly IRoomService _roomService;
        private readonly ITraxService _traxService;
        private readonly IFriendService _friendService;
        private readonly ITagService _tagService;

        public HomeService(DataContext context, IUserService userService, IRoomService roomService, ITraxService traxService, IFriendService friendService, ITagService tagService)
        {
            _context = context;
            _userService = userService;
            _roomService = roomService;
            _traxService = traxService;
            _friendService = friendService;
            _tagService = tagService;
        }

        // ---------------------------------------------------------------- viewing

        public async Task<HomeViewModel> GetHome(int userId, bool enableEditing, int? currentUserId = null)
        {
            var homeUser = await _userService.GetUserById(userId);
            if (homeUser == null) return null;

            var home = await InitHome(homeUser.Id);
            var model = await BuildHomeViewModel(home, enableEditing, currentUserId);
            model.HomeUser = homeUser;
            return model;
        }

        public async Task<HomeViewModel> GetHomeByGroupName(string groupUrl, bool enableEditing, int? currentUserId = null)
        {
            if (string.IsNullOrEmpty(groupUrl)) return null;
            groupUrl = groupUrl.ToLower();
            var group = await _context.Homes.FirstOrDefaultAsync(s => s.Type == GroupHome && s.GroupUrl == groupUrl);
            return group == null ? null : await BuildHomeViewModel(group, enableEditing, currentUserId);
        }

        public async Task<HomeViewModel> GetHomeByGroupId(int groupId, bool enableEditing, int? currentUserId = null)
        {
            var home = await GetHomeDetailsById(groupId);
            return home == null ? null : await BuildHomeViewModel(home, enableEditing, currentUserId);
        }

        private async Task<HomeViewModel> BuildHomeViewModel(Homes home, bool enableEditing, int? currentUserId)
        {
            var widgetData = await GetWidgetData(home.Id, currentUserId);
            var items = await (from item in _context.HomesItems
                               join def in _context.HomesItemData on item.ItemId equals def.Id
                               where item.HomeId == home.Id
                               select new ItemViewModel { Item = item, Definition = def, EnableEditing = enableEditing })
                              .ToListAsync();
            items.ForEach(i => i.WidgetData = widgetData);

            return new HomeViewModel
            {
                Home = home,
                Items = items,
                IsEditing = enableEditing,
                CanEdit = widgetData.CanEdit,
                Membership = currentUserId != null && home.Type == GroupHome ? await GetMembership(home.Id, currentUserId.Value) : null,
            };
        }

        public Task<Homes> GetHomeDetailsById(int homeId)
        {
            return _context.Homes.FirstOrDefaultAsync(s => s.Id == homeId);
        }

        public async Task<ItemViewModel> GetItem(int id, bool enableEditing = false)
        {
            return await (from item in _context.HomesItems
                          join def in _context.HomesItemData on item.ItemId equals def.Id
                          where item.Id == id
                          select new ItemViewModel { Item = item, Definition = def, EnableEditing = enableEditing })
                         .FirstOrDefaultAsync();
        }

        /// <summary>An item of this home (null for an item of any other home).</summary>
        private async Task<ItemViewModel> GetHomeItem(int homeId, int itemId, bool enableEditing = true)
        {
            var item = await GetItem(itemId, enableEditing);
            return item != null && item.Item.HomeId == homeId ? item : null;
        }

        public async Task<ItemWidgetDataModel> GetWidgetData(int homeId, int? userId)
        {
            var home = await GetHomeDetailsById(homeId);
            if (home == null) return null;

            var canEdit = await CanEditHome(homeId, userId);
            var data = new ItemWidgetDataModel
            {
                Home = home,
                User = await _userService.GetUserById(home.UserId),
                SongList = await _traxService.GetSongsByOwner(home.UserId),
                Guestbook = await GetGuestbook(homeId),
                CanEdit = canEdit,
            };

            if (home.Type == GroupHome)
            {
                data.Tags = await _tagService.TagsForGroup(home.Id, canEdit);
                data.GroupMembers = await GetGroupMembers(home);
                data.GroupRoom = await GetGroupRoom(home);
            }
            else
            {
                data.Tags = await _tagService.TagsForUser(home.UserId, canEdit);
                data.Rooms = await _roomService.GetRoomsByOwner(home.UserId);
                data.Ratings = await GetRatings(home.Id);
                data.Badges = await _context.UsersBadges.Where(s => s.UserId == home.UserId).ToListAsync();
                data.Friends = await _friendService.GetFriendsWithUserData(home.UserId);
                data.Groups = await GetGroupsForUser(home.UserId);
                data.WobbleSquabblePoints = await _context.Highscores
                    .Where(h => h.UserId == home.UserId && h.GameType == "wobblesquabble")
                    .SumAsync(h => (int?)h.Score) ?? 0;
                // The guestbook widget checks group membership for private guestbooks on every home type.
                data.GroupMembers = new List<GroupViewModel>();
            }
            return data;
        }

        public async Task<bool> CanEditHome(int homeId, int? userId)
        {
            if (userId == null) return false;

            var home = await GetHomeDetailsById(homeId);
            if (home == null) return false;
            if (home.UserId == userId) return true;

            return home.Type == GroupHome
                && await _context.GroupMembers.AnyAsync(s => s.GroupId == homeId && s.UserId == userId && s.IRights == 1 && !s.Pending);
        }

        // ---------------------------------------------------------------- editing a home

        public async Task<bool> Save(int homeId, int currentUserId, SaveModel data)
        {
            var home = await GetHomeDetailsById(homeId);
            if (home == null || data == null) return false;

            var moves = SaveDataStringConverter.GetItemsFromString(data.Stickers)
                .Concat(SaveDataStringConverter.GetItemsFromString(data.Widgets))
                .Concat(SaveDataStringConverter.GetItemsFromString(data.StickieNotes))
                .GroupBy(m => m.Id)
                .ToDictionary(g => g.Key, g => g.Last());

            // Only items of this home move, whatever ids the request names.
            var items = await _context.HomesItems.Where(i => i.HomeId == homeId && moves.Keys.Contains(i.Id)).ToListAsync();
            foreach (var item in items)
            {
                var move = moves[item.Id];
                item.X = move.X;
                item.Y = move.Y;
                item.Z = move.Z;
            }

            // "background: <inventory id>:<css class>", a background the editor owns.
            var backgroundId = data.Background?.Split(':')[0];
            if (int.TryParse(backgroundId, out var inventoryId))
            {
                var background = await GetInventoryItem(inventoryId, currentUserId);
                if (background != null && background.Definition.Type == Backgrounds)
                {
                    home.Background = background.Definition.CssClass;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ItemViewModel> PlaceItem(int homeId, int inventoryId, int z, int userId, string type)
        {
            var home = await GetHomeDetailsById(homeId);
            if (home == null || (type != Widgets && type != "stickers")) return null;

            var stack = await FindPlaceableStack(home, inventoryId, userId, type);
            if (stack == null) return null;

            var definition = await GetItemDetail(stack.ItemId);
            if (definition == null || definition.Type != type) return null;

            TakeOne(stack, definition);
            var newItem = new HomesItems { HomeId = home.Id, OwnerId = userId, ItemId = stack.ItemId, X = 0, Y = 0, Z = z, Skin = DefaultSkin };
            _context.HomesItems.Add(newItem);
            await _context.SaveChangesAsync();

            return new ItemViewModel { Item = newItem, Definition = definition, EnableEditing = true, WidgetData = await GetWidgetData(homeId, userId) };
        }

        public async Task<ItemViewModel> PlaceNote(int homeId, int skin, string text, int userId)
        {
            var home = await GetHomeDetailsById(homeId);
            if (home == null) return null;

            var stack = await (from inv in PersonalInventory(userId)
                               join def in _context.HomesItemData on inv.ItemId equals def.Id
                               where def.Type == Notes && inv.Amount > 0
                               select new InventoryItem { Details = inv, Definition = def })
                              .FirstOrDefaultAsync();
            if (stack == null) return null;

            TakeOne(stack.Details, stack.Definition);
            var note = new HomesItems
            {
                HomeId = home.Id,
                OwnerId = userId,
                ItemId = stack.Definition.Id,
                X = 0, Y = 0, Z = 0,
                Skin = SkinIdToString.ConvertNote(skin),
                Data = Truncate(text, MaxNoteLength),
            };
            _context.HomesItems.Add(note);
            await _context.SaveChangesAsync();

            return new ItemViewModel { Item = note, Definition = stack.Definition, EnableEditing = true, WidgetData = await GetWidgetData(homeId, userId) };
        }

        public async Task<HomesInventory> RemoveItem(int homeId, int itemId, int userId)
        {
            var home = await GetHomeDetailsById(homeId);
            var item = await GetHomeItem(homeId, itemId);
            if (home == null || item == null) return null;

            var stack = await ReturnToInventory(item.Item, item.Definition, home);
            _context.HomesItems.Remove(item.Item);
            await _context.SaveChangesAsync();
            return stack;
        }

        public async Task<ItemViewModel> EditItem(int homeId, int itemId, int skinId, int userId)
        {
            var item = await GetHomeItem(homeId, itemId);
            if (item == null) return null;

            // The Habbo Club skins are for club members, as in the skin menu.
            if (SkinIdToString.IsClubSkin(skinId) && (await _userService.GetUserById(userId))?.HasHabboClub != true)
            {
                return null;
            }

            item.Item.Skin = item.Definition.Type == Notes ? SkinIdToString.ConvertNote(skinId) : SkinIdToString.Convert(skinId);
            await _context.SaveChangesAsync();

            item.WidgetData = await GetWidgetData(homeId, userId);
            return item;
        }

        public async Task<ItemViewModel> SelectSong(int homeId, int itemId, int songId, int userId)
        {
            var home = await GetHomeDetailsById(homeId);
            var item = await GetHomeItem(homeId, itemId);
            if (home == null || item == null) return null;

            // The player plays one of the home owner's songs, the list the widget offers.
            var song = await _traxService.GetSingleSongById(songId);
            item.Item.Data = song != null && song.UserId == home.UserId ? song.Id.ToString() : null;
            await _context.SaveChangesAsync();

            item.WidgetData = await GetWidgetData(homeId, userId);
            return item;
        }

        public async Task<List<InventoryItem>> GetInventory(int homeId, string type, int userId)
        {
            var home = await GetHomeDetailsById(homeId);
            if (home == null) return new List<InventoryItem>();

            var stacks = home.Type == GroupHome && type == Widgets
                ? _context.HomesInventory.Where(s => s.HomeId == homeId)
                : PersonalInventory(userId);

            return await (from inv in stacks
                          join def in _context.HomesItemData on inv.ItemId equals def.Id
                          where def.Type == type
                          select new InventoryItem { Details = inv, Definition = def })
                         .ToListAsync();
        }

        public async Task<InventoryItem> GetInventoryItem(int inventoryId, int userId)
        {
            return await (from inv in PersonalInventory(userId)
                          join def in _context.HomesItemData on inv.ItemId equals def.Id
                          where inv.Id == inventoryId
                          select new InventoryItem { Details = inv, Definition = def })
                         .FirstOrDefaultAsync();
        }

        // ---------------------------------------------------------------- inventory

        /// <summary>
        /// A user's own stacks: home_id 0, or (in data from before this rule) their own user home's id.
        /// Stacks of a group home belong to that group.
        /// </summary>
        private IQueryable<HomesInventory> PersonalInventory(int userId)
        {
            return _context.HomesInventory.Where(s => s.UserId == userId
                && (s.HomeId == 0 || _context.Homes.Any(h => h.Id == s.HomeId && h.Type == UserHome)));
        }

        /// <summary>
        /// A stack the user can place from: their own stickers anywhere, their own widgets in their user home and
        /// the group's widgets in a group home. (A user's profile, badge or friends widget breaks a group page.)
        /// </summary>
        private Task<HomesInventory> FindPlaceableStack(Homes home, int inventoryId, int userId, string type)
        {
            var stacks = type == Widgets && home.Type == GroupHome
                ? _context.HomesInventory.Where(s => s.HomeId == home.Id)
                : PersonalInventory(userId);
            return stacks.FirstOrDefaultAsync(s => s.Id == inventoryId && s.Amount > 0);
        }

        /// <summary>Takes one item off a stack. A widget stack stays at 0, so it lists as placed and can't be bought again.</summary>
        private void TakeOne(HomesInventory stack, HomesItemData definition)
        {
            if (definition.Type == Widgets || stack.Amount > 1)
            {
                stack.Amount = Math.Max(0, stack.Amount - 1);
            }
            else
            {
                _context.HomesInventory.Remove(stack);
            }
        }

        /// <summary>
        /// Puts a removed item back: a group's widget into that group's stack, anything else into the stack of the
        /// user who placed it (not whoever removes it).
        /// </summary>
        private async Task<HomesInventory> ReturnToInventory(HomesItems item, HomesItemData definition, Homes home)
        {
            var groupWidget = home.Type == GroupHome && definition.Type == Widgets;
            var stack = groupWidget
                ? await _context.HomesInventory.FirstOrDefaultAsync(s => s.HomeId == home.Id && s.ItemId == item.ItemId)
                : await PersonalInventory(item.OwnerId).FirstOrDefaultAsync(s => s.ItemId == item.ItemId);

            if (stack != null)
            {
                stack.Amount++;
                return stack;
            }

            stack = new HomesInventory
            {
                Amount = 1,
                ItemId = item.ItemId,
                UserId = groupWidget ? home.UserId : item.OwnerId,
                HomeId = groupWidget ? home.Id : 0,
            };
            _context.HomesInventory.Add(stack);
            return stack;
        }

        // ---------------------------------------------------------------- store

        public Task<List<HomesCategories>> GetStoreCategories()
        {
            return _context.HomesCategories.Where(c => c.IEnabled == 1).ToListAsync();
        }

        public Task<List<CatalogItem>> GetCatelogItemsInCategory(int category)
        {
            return (from item in _context.HomesCatalog
                    join def in _context.HomesItemData on item.ItemId equals def.Id
                    where item.Category == category
                    select new CatalogItem { Details = item, Definition = def })
                   .ToListAsync();
        }

        public async Task<List<CatalogItem>> GetStoreCatelog(int categoryId, int subCategoryId)
        {
            var categories = await GetStoreCategories();
            var category = categories.FirstOrDefault(s => s.Id == categoryId);
            if (category == null) return new List<CatalogItem>();

            var subCategory = categories.FirstOrDefault(s => s.Id == subCategoryId);
            // A sticker category without a sub category chosen shows its first child.
            var shown = subCategory
                ?? (category.Type == "stickers" ? categories.FirstOrDefault(s => s.ParentId == category.Id) : null)
                ?? category;
            return await GetCatelogItemsInCategory(shown.Id);
        }

        public async Task<CatalogItem> GetProduct(int productId)
        {
            return await (from item in _context.HomesCatalog
                          join def in _context.HomesItemData on item.ItemId equals def.Id
                          join category in _context.HomesCategories on item.Category equals category.Id
                          where item.Id == productId && category.IEnabled == 1
                          select new CatalogItem { Details = item, Definition = def })
                         .FirstOrDefaultAsync();
        }

        public Task<HomesItemData> GetItemDetail(int id)
        {
            return _context.HomesItemData.FirstOrDefaultAsync(s => s.Id == id);
        }

        public Task<HomesItemData> GetItemDataByClass(string className)
        {
            return _context.HomesItemData.FirstOrDefaultAsync(s => s.CssClass == className);
        }

        public async Task<bool> CanReceiveItem(int itemId, int userId)
        {
            var definition = await GetItemDetail(itemId);
            if (definition == null) return false;
            return definition.Type != Backgrounds || !await PersonalInventory(userId).AnyAsync(s => s.ItemId == itemId);
        }

        public async Task<bool> GiveItem(int itemId, int amount, int userId)
        {
            if (amount < 1 || !await CanReceiveItem(itemId, userId)) return false;

            // Add in one UPDATE: two purchases at once both read the old amount and one of them was lost.
            var stackId = await PersonalInventory(userId).Where(s => s.ItemId == itemId).OrderBy(s => s.Id).Select(s => (int?)s.Id).FirstOrDefaultAsync();
            if (stackId != null)
            {
                await _context.HomesInventory.Where(s => s.Id == stackId)
                    .ExecuteUpdateAsync(set => set.SetProperty(s => s.Amount, s => s.Amount + amount));
            }
            else
            {
                _context.HomesInventory.Add(new HomesInventory { Amount = amount, UserId = userId, ItemId = itemId, HomeId = 0 });
                await _context.SaveChangesAsync();
            }
            return true;
        }

        // ---------------------------------------------------------------- ratings

        public async Task<ItemViewModel> Rate(int rating, int itemId, int userId)
        {
            var item = await GetItem(itemId);
            if (item == null) return null;
            var home = await GetHomeDetailsById(item.Item.HomeId);
            if (home == null) return null;

            var alreadyRated = await _context.HomesRating.AnyAsync(s => s.HomeId == home.Id && s.UserId == userId);
            if (rating >= 1 && rating <= 5 && home.UserId != userId && !alreadyRated)
            {
                _context.HomesRating.Add(new HomesRating { UserId = userId, HomeId = home.Id, Rating = rating });
                await _context.SaveChangesAsync();
            }

            item.WidgetData = await GetWidgetData(home.Id, userId);
            return item;
        }

        public async Task<ItemViewModel> ResetRating(int homeId, int itemId, int userId)
        {
            var item = await GetHomeItem(homeId, itemId, false);
            var home = await GetHomeDetailsById(homeId);
            if (item == null || home == null) return null;

            if (home.UserId == userId)
            {
                _context.HomesRating.RemoveRange(_context.HomesRating.Where(s => s.HomeId == homeId));
                await _context.SaveChangesAsync();
            }

            item.WidgetData = await GetWidgetData(homeId, userId);
            return item;
        }

        public Task<List<HomesRating>> GetRatings(int homeId)
        {
            return _context.HomesRating.Where(s => s.HomeId == homeId).ToListAsync();
        }

        // ---------------------------------------------------------------- guestbook

        public async Task<GuestbookEntry> AddGuestbookEntry(int homeId, string message, int userId)
        {
            var home = await GetHomeDetailsById(homeId);
            var user = await _userService.GetUserById(userId);
            message = message?.Trim();
            if (home == null || user == null || string.IsNullOrEmpty(message)) return null;
            if (!await CanWriteInGuestbook(home, userId)) return null;

            var entry = new HomesGuestbook { UserId = userId, HomeId = homeId, Message = Truncate(message, MaxGuestbookLength), Timestamp = DateTime.Now };
            _context.HomesGuestbook.Add(entry);
            await _context.SaveChangesAsync();
            return new GuestbookEntry { Entry = entry, User = user };
        }

        /// <summary>A private guestbook takes entries from the owner, editors, friends (user home) and members (group home).</summary>
        private async Task<bool> CanWriteInGuestbook(Homes home, int userId)
        {
            var isPrivate = await (from item in _context.HomesItems
                                   join def in _context.HomesItemData on item.ItemId equals def.Id
                                   where item.HomeId == home.Id && def.CssClass == "w_guestbookwidget"
                                   select item.Data).AnyAsync(d => d == "private");
            if (!isPrivate || await CanEditHome(home.Id, userId)) return true;

            return home.Type == GroupHome
                ? await _context.GroupMembers.AnyAsync(m => m.GroupId == home.Id && m.UserId == userId && !m.Pending)
                : await _friendService.IsFriends(userId, home.UserId);
        }

        /// <summary>The author or someone who can edit the home removes an entry.</summary>
        public async Task<HomesGuestbook> DeleteGuestbookEntry(int id, int userId)
        {
            var entry = await _context.HomesGuestbook.FirstOrDefaultAsync(s => s.Id == id);
            if (entry == null) return null;
            if (entry.UserId != userId && !await CanEditHome(entry.HomeId, userId)) return null;

            _context.HomesGuestbook.Remove(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<List<GuestbookEntry>> GetGuestbook(int homeId)
        {
            var entries = await _context.HomesGuestbook.Where(s => s.HomeId == homeId).OrderByDescending(s => s.Timestamp).ToListAsync();
            var authorIds = entries.Select(e => e.UserId).Distinct().ToList();
            var authors = await _context.Users.Where(u => authorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

            return entries
                .Where(e => authors.ContainsKey(e.UserId))
                .Select(e => new GuestbookEntry { Entry = e, User = authors[e.UserId] })
                .ToList();
        }

        public async Task<ItemViewModel> ConfigureGuestbook(int widgetId, int userId)
        {
            var item = await GetItem(widgetId);
            if (item == null || !await CanEditHome(item.Item.HomeId, userId)) return null;

            item.Item.Data = item.Item.Data == "private" ? "" : "private";
            await _context.SaveChangesAsync();
            return item;
        }

        // ---------------------------------------------------------------- creating homes

        public async Task<Homes> InitHome(int userId)
        {
            var home = await FindUserHome(userId);
            if (home != null) return home;

            await HomeCreation.WaitAsync();
            try
            {
                home = await FindUserHome(userId);
                if (home != null) return home;

                home = new Homes { Created = DateTime.Now, UserId = userId, AllowDisplay = true, Background = DefaultBackground, Type = UserHome };
                _context.Homes.Add(home);
                await _context.SaveChangesAsync();

                await AddItemByClass(home, "s_needle_3", userId, 106, 31, 564);
                await AddItemByClass(home, "s_sticker_spaceduck", userId, 257, 340, 582);
                await AddItemByClass(home, "s_paper_clip_1", userId, 167, 411, 576);
                await AddItemByClass(home, "commodity_stickienote", userId, 124, 49, 550, "n_skin_noteitskin", DbRes.T("default_note1", "habbohome"));
                await AddItemByClass(home, "commodity_stickienote", userId, 31, 247, 568, "n_skin_speechbubbleskin", DbRes.T("default_note2", "habbohome"));
                await AddItemByClass(home, "commodity_stickienote", userId, 115, 447, 570, "n_skin_notepadskin", DbRes.T("default_note3", "habbohome"));
                await AddItemByClass(home, "w_profilewidget", userId, 445, 28, 546);
                await AddItemByClass(home, "w_roomswidget", userId, 428, 291, 558);
                await AddWidgetsToInventory(userId, 0, "w_ratingwidget", "w_highscoreswidget", "w_guestbookwidget", "w_friendswidget", "w_badgeswidget", "w_traxplayerwidget");

                await _context.SaveChangesAsync();
                return home;
            }
            finally
            {
                HomeCreation.Release();
            }
        }

        private Task<Homes> FindUserHome(int userId)
        {
            return _context.Homes.Where(s => s.UserId == userId && s.Type == UserHome).OrderBy(s => s.Id).FirstOrDefaultAsync();
        }

        public async Task<Homes> InitGroup(string name, string description, int userId)
        {
            var group = new Homes
            {
                GroupBadge = "b0503Xs09114s05013s05015",
                Created = DateTime.Now,
                GroupName = name,
                GroupDescription = description,
                UserId = userId,
                AllowDisplay = true,
                Background = DefaultBackground,
                Type = GroupHome,
            };
            _context.Homes.Add(group);
            await _context.SaveChangesAsync();

            await AddItemByClass(group, "w_groupinfowidget", userId, 445, 28, 546);
            await AddItemByClass(group, "w_memberwidget", userId, 428, 291, 558);
            await AddWidgetsToInventory(userId, group.Id, "w_guestbookwidget", "w_traxplayerwidget");

            await _context.SaveChangesAsync();
            return group;
        }

        private async Task AddItemByClass(Homes home, string cssClass, int ownerId, int x, int y, int z, string skin = DefaultSkin, string data = null)
        {
            var definition = await GetItemDataByClass(cssClass);
            if (definition == null) return;
            _context.HomesItems.Add(new HomesItems { HomeId = home.Id, ItemId = definition.Id, OwnerId = ownerId, X = x, Y = y, Z = z, Skin = skin, Data = data });
        }

        private async Task AddWidgetsToInventory(int userId, int homeId, params string[] cssClasses)
        {
            var widgets = await _context.HomesItemData.Where(s => cssClasses.Contains(s.CssClass)).ToListAsync();
            foreach (var widget in widgets)
            {
                _context.HomesInventory.Add(new HomesInventory { Amount = 1, ItemId = widget.Id, UserId = userId, HomeId = homeId });
            }
        }

        // ---------------------------------------------------------------- groups

        public async Task<List<GroupViewModel>> GetGroupsForUser(int userId)
        {
            var groups = (await _context.Homes.Where(s => s.Type == GroupHome && s.UserId == userId).ToListAsync())
                .Select(home => new GroupViewModel { Home = home, GroupMember = null })
                .ToList();

            groups.AddRange(await (from member in _context.GroupMembers
                                   join home in _context.Homes on member.GroupId equals home.Id
                                   where member.UserId == userId
                                   select new GroupViewModel { Home = home, GroupMember = member }).ToListAsync());
            return groups;
        }

        public async Task<List<GroupViewModel>> GetGroupMembers(Homes home)
        {
            var members = new List<GroupViewModel>
            {
                new GroupViewModel { Home = home, GroupMember = new GroupMembers { Owner = true, User = await _userService.GetUserById(home.UserId) } },
            };
            members.AddRange((await GetGroupMembers(home.Id)).Select(m => new GroupViewModel { Home = home, GroupMember = m }));
            return members;
        }

        public Task<List<GroupMembers>> GetGroupMembers(int groupId)
        {
            return (from member in _context.GroupMembers
                    join user in _context.Users on member.UserId equals user.Id
                    where member.GroupId == groupId
                    orderby member.IRights
                    select new GroupMembers
                    {
                        GroupId = member.GroupId,
                        Id = member.Id,
                        Pending = member.Pending,
                        User = user,
                        Rights = member.Rights,
                        UserId = member.UserId,
                    }).ToListAsync();
        }

        public Task<Rooms> GetGroupRoom(Homes home)
        {
            return _context.Rooms.FirstOrDefaultAsync(s => s.Id == home.GroupRoom);
        }

        public async Task<Homes> UpdateGroupBadge(int homeId, string code, int userId)
        {
            var home = await GetHomeDetailsById(homeId);
            if (home != null && !string.IsNullOrEmpty(code) && await CanEditHome(homeId, userId))
            {
                home.GroupBadge = code;
                await _context.SaveChangesAsync();
            }
            return home;
        }

        public async Task<bool> IsGroupUrlValid(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            url = url.ToLower();
            return !await _context.Homes.AnyAsync(s => s.GroupUrl == url);
        }

        public async Task<Homes> UpdateGroup(int groupId, string name, string description, string url, int type, int? roomId)
        {
            var group = await GetHomeDetailsById(groupId);
            if (group == null) return null;

            group.GroupName = string.IsNullOrEmpty(name) ? group.GroupName : name;
            group.GroupDescription = description;
            // The URL is set once.
            if (string.IsNullOrEmpty(group.GroupUrl) && await IsGroupUrlValid(url))
            {
                group.GroupUrl = url.ToLower();
            }
            group.GroupType = type;
            group.GroupRoom = roomId;

            await _context.SaveChangesAsync();
            return group;
        }

        public async Task<bool> DeleteGroup(int groupId)
        {
            var group = await GetHomeDetailsById(groupId);
            if (group == null || group.Type != GroupHome) return false;

            foreach (var user in await _context.Users.Where(u => u.Group == groupId).ToListAsync())
            {
                user.Group = 0;
            }
            _context.GroupMembers.RemoveRange(_context.GroupMembers.Where(m => m.GroupId == groupId));

            // Stickers and notes go back to the users who placed them; the group's widgets go with the group.
            var items = await (from item in _context.HomesItems
                               join def in _context.HomesItemData on item.ItemId equals def.Id
                               where item.HomeId == groupId
                               select new { Item = item, Definition = def }).ToListAsync();
            foreach (var placed in items)
            {
                if (placed.Definition.Type != Widgets)
                {
                    await ReturnToInventory(placed.Item, placed.Definition, group);
                }
                _context.HomesItems.Remove(placed.Item);
            }
            _context.HomesInventory.RemoveRange(_context.HomesInventory.Where(s => s.HomeId == groupId));
            _context.Homes.Remove(group);

            await _context.SaveChangesAsync();
            return true;
        }

        public Task<GroupMembers> GetMembership(int groupId, int userId)
        {
            return _context.GroupMembers.FirstOrDefaultAsync(s => s.GroupId == groupId && s.UserId == userId);
        }

        public Task<List<GroupMembers>> AcceptMembers(int groupId, int[] targetIds)
        {
            return UpdateMembers(groupId, targetIds, member => member.Pending = false);
        }

        public Task<List<GroupMembers>> GiveGroupRights(int groupId, int[] targetIds)
        {
            return UpdateMembers(groupId, targetIds, member => member.IRights = 1);
        }

        public Task<List<GroupMembers>> RemoveGroupRights(int groupId, int[] targetIds)
        {
            return UpdateMembers(groupId, targetIds, member => member.IRights = 0);
        }

        public async Task<List<GroupMembers>> RemoveMembers(int groupId, int[] targetIds)
        {
            var members = await MembersOf(groupId, targetIds);
            foreach (var member in members)
            {
                _context.GroupMembers.Remove(member);
                var user = await _userService.GetUserById(member.UserId);
                if (user != null && user.Group == groupId)
                {
                    await _userService.SetGroup(member.UserId, 0);
                }
            }
            await _context.SaveChangesAsync();
            return members;
        }

        private async Task<List<GroupMembers>> UpdateMembers(int groupId, int[] targetIds, Action<GroupMembers> change)
        {
            var members = await MembersOf(groupId, targetIds);
            members.ForEach(change);
            await _context.SaveChangesAsync();
            return members;
        }

        private Task<List<GroupMembers>> MembersOf(int groupId, int[] memberIds)
        {
            memberIds ??= Array.Empty<int>();
            return _context.GroupMembers.Where(s => s.GroupId == groupId && memberIds.Contains(s.Id)).ToListAsync();
        }

        public async Task<GroupMembers> AddPendingMember(int groupId, int userId)
        {
            var membership = new GroupMembers { GroupId = groupId, Pending = true, UserId = userId };
            _context.GroupMembers.Add(membership);
            await _context.SaveChangesAsync();
            return membership;
        }

        public Task<List<Homes>> GetRecentGroups()
        {
            return _context.Homes.Where(s => s.Type == GroupHome).OrderByDescending(s => s.Created).ToListAsync();
        }

        public Task<List<Homes>> GetHotelGroups()
        {
            return _context.Homes.Where(s => s.Type == GroupHome).OrderByDescending(s => s.GroupName).ToListAsync();
        }

        public Task<List<Homes>> GetActiveGroups()
        {
            return _context.Homes.Where(s => s.Type == GroupHome).OrderBy(s => s.Created).ToListAsync();
        }

        private static string Truncate(string text, int length)
        {
            return text != null && text.Length > length ? text.Substring(0, length) : text;
        }
    }
}
