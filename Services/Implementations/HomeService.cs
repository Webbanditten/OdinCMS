using KeplerCMS.Areas.MyHabbo.Helpers;
using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Westwind.Globalization;

namespace KeplerCMS.Services.Implementations
{
    public class HomeService : IHomeService
    {
        private readonly DataContext _context;
        private readonly IUserService _userService;
        private readonly IRoomService _roomService;
        private readonly ITraxService _traxService;
        private readonly IFriendService _friendService;

        public HomeService(DataContext context, IUserService userService, IRoomService roomService, ITraxService traxService, IFriendService friendService)
        {
            _userService = userService;
            _context = context;
            _roomService = roomService;
            _traxService = traxService;
            _friendService = friendService;
        }
        public async Task<HomeViewModel> GetHome(int userId, bool enableEditing)
        {
            // Get home for type user
            var homeUser = await _userService.GetUserById(userId.ToString());

            Homes home = (homeUser != null) ? await InitHome(homeUser.Id) : null;
            var homeViewModel = new HomeViewModel { Home = home, Items = new List<ItemViewModel>(), HomeUser = homeUser, IsEditing = enableEditing };

            var widgetData = await GetWidgetData(homeUser.Id);

            homeViewModel.Items = await (from item in _context.HomesItems
                                              join def in _context.HomesItemData
                                              on item.ItemId equals def.Id
                                              where item.OwnerId == userId
                                              select new ItemViewModel
                                              {
                                                  Item = item,
                                                  EnableEditing = enableEditing,
                                                  Definition = def,
                                                  WidgetData = widgetData
                                              }).ToListAsync();

            return homeViewModel;
        }

        public async Task<Homes> GetHomeDetailsById(int homeId)
        {
            return await _context.Homes.Where(s => s.Id == homeId).FirstOrDefaultAsync();
        }

        public async Task<List<HomesCategories>> GetStoreCategories()
        {
            return await _context.HomesCategories.ToListAsync(); 
        }

        public async Task<List<CatalogItem>> GetCatelogItemsInCategory(int category)
        {
            List<CatalogItem> data = await (from item in _context.HomesCatalog
                                      join def in _context.HomesItemData
                                      on item.ItemId equals def.Id
                                      where item.Category == category
                                      select new CatalogItem
                                      {
                                          Details = item,
                                          Definition = def
                                      }).ToListAsync();
            return data;
        }

        public async Task<List<CatalogItem>> GetStoreCatelog(int categoryId, int subCategoryId)
        {
            var categories = await _context.HomesCategories.ToListAsync();
            var category = categories.Where(s => s.Id == categoryId).FirstOrDefault();
            var subCategory = categories.Where(s => s.Id == subCategoryId).FirstOrDefault();
            if(category != null)
            {
                var items = new List<CatalogItem>();

                if (subCategory != null)
                {
                    items.AddRange(await GetCatelogItemsInCategory(subCategory.Id));
                } else
                {
                    // If the category is stickers we might want to see if the category has any children
                    if (category.Type == "stickers")
                    {
                        var firstchild = categories.Where(s => s.ParentId == category.Id).FirstOrDefault();
                        if(firstchild != null)
                        {
                            items.AddRange(await GetCatelogItemsInCategory(firstchild.Id));
                        } else
                        {
                            items.AddRange(await GetCatelogItemsInCategory(category.Id));
                        }
                    } else
                    {
                        items.AddRange(await GetCatelogItemsInCategory(category.Id));
                    }
                }
                return items;
            }
            return null;
        }

        public async Task<ItemViewModel> PlaceItem(int inventoryId, int z, int userId, string data = null)
        {
            var item = await _context.HomesInventory.Where(s => s.Id == inventoryId && s.UserId == userId).FirstOrDefaultAsync();
            var homeForUser = await _context.Homes.Where(s => s.UserId == userId).FirstOrDefaultAsync();
            HomesItems newItem = null;
            if(item != null && homeForUser != null)
            {
                var def = await GetItemDetail(item.ItemId);
                if(def.Type == "widgets")
                {
                    item.Amount = 0;
                    _context.HomesInventory.Update(item);
                } else
                {
                    if (item.Amount > 1)
                    {
                        item.Amount--;
                        _context.HomesInventory.Update(item);
                    }
                    else
                    {
                        _context.HomesInventory.Remove(item);
                    }
                }


                newItem = new HomesItems { HomeId = homeForUser.Id, OwnerId = userId, ItemId = item.ItemId, X = 0, Z = z, Y = 0, Skin = "w_skin_defaultskin" };
                _context.HomesItems.Add(newItem);
                await _context.SaveChangesAsync();
            }

            return new ItemViewModel { Item = newItem, Definition = await GetItemDetail(newItem.ItemId), EnableEditing = true, WidgetData = await GetWidgetData(homeForUser.UserId)  };
        }

        public async Task<HomesInventory> RemoveItem(int itemId, int userId)
        {
            var currentItem = await _context.HomesItems.Where(s => s.Id == itemId && s.OwnerId == userId).FirstOrDefaultAsync();
            var inventoryItem = await _context.HomesInventory.Where(s => s.ItemId == currentItem.ItemId).FirstOrDefaultAsync();
            HomesInventory newInventoryItem = null;
            if(currentItem != null)
            {
                if (inventoryItem != null)
                {
                    inventoryItem.Amount++;
                    _context.HomesInventory.Update(inventoryItem);
                }
                else
                {
                    newInventoryItem = new HomesInventory { Amount = 1, ItemId = currentItem.ItemId, UserId = userId };
                    _context.HomesInventory.Add(newInventoryItem);
                }
            }
            _context.HomesItems.Remove(currentItem);
            await _context.SaveChangesAsync();
            return newInventoryItem;
        }


        public async Task<bool> Save(int userId, SaveModel data)
        {
            var user = await _userService.GetUserById(userId.ToString());
            var home = await _context.Homes.Where(s => s.UserId == user.Id).FirstOrDefaultAsync();
            if (user == null || home == null) return false;

            var itemsChanged = new List<HomesItems>();
            itemsChanged.AddRange(SaveDataStringConverter.GetItemsFromString(data.Stickers));
            itemsChanged.AddRange(SaveDataStringConverter.GetItemsFromString(data.Widgets));
            itemsChanged.AddRange(SaveDataStringConverter.GetItemsFromString(data.StickieNotes));
            if(data.Background != null)
            {
                var bgData = data.Background.Split(":");
                var bgItemId = bgData[0];
                var dbItem = GetInventoryItem(int.Parse(bgItemId), user.Id);
                // If I plan to make groups work the following line needs to be fixed
                if(dbItem != null && home != null)
                {
                    home.Background = dbItem.Definition.CssClass;
                    _context.Homes.Update(home);
                }
            }
            foreach (var item in itemsChanged)
            {
                var dbItem = await _context.HomesItems.Where(s=>s.Id == item.Id && s.OwnerId == userId).FirstOrDefaultAsync();
                if(dbItem != null)
                {
                    // Lets check if the home actually exists
                    if (home != null && home.UserId == user.Id)
                    {
                        dbItem.X = item.X;
                        dbItem.Y = item.Y;
                        dbItem.Z = item.Z;
                        _context.HomesItems.Update(dbItem);
                    }
                }

            }
            _context.SaveChanges();
            return true;
        }

        public async Task<HomesItemData> GetItemDetail(int id)
        {
            return await _context.HomesItemData.Where(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task<CatalogItem> GetProduct(int productId)
        {
            var product = await _context.HomesCatalog.Where(s => s.Id == productId).FirstOrDefaultAsync();
            if (product == null) return null;

            var itemDef = await GetItemDetail(product.ItemId);
            if (itemDef == null) return null;

            return new CatalogItem { Definition = itemDef, Details = product };
        }

        public async Task<bool> GiveItem(int itemId, int amount, int userId)
        {
            var similarProductInInventory = await _context.HomesInventory.Where(s => s.ItemId == itemId).FirstOrDefaultAsync();
            if(similarProductInInventory != null)
            {
                var itemDef = await GetItemDetail(similarProductInInventory.ItemId);
                if (itemDef == null) return false;
                if(itemDef.Type == "backgrounds") return false;
                
                similarProductInInventory.Amount += similarProductInInventory.Amount;
                _context.HomesInventory.Update(similarProductInInventory);
            } else
            {
                _context.HomesInventory.Add(new HomesInventory { Amount = amount, UserId = userId, ItemId = itemId });
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<InventoryItem>> GetInventory(string type, int userId)
        {

            List<InventoryItem> data = await (from item in _context.HomesInventory
                                            join def in _context.HomesItemData
                                            on item.ItemId equals def.Id
                                            where def.Type == type && item.UserId == userId
                                            select new InventoryItem
                                              {
                                                Details = item,
                                                Definition = def
                                            }).ToListAsync();

            return data;
        }

        public InventoryItem GetInventoryItem(int inventoryId, int userId)
        {
            return (from item in _context.HomesInventory
                   join def in _context.HomesItemData
                   on item.ItemId equals def.Id
                   where item.Id == inventoryId && item.UserId == userId
                   select new InventoryItem
                   {
                       Details = item,
                       Definition = def
                   }).FirstOrDefault();
        }

        public async Task<ItemViewModel> EditItem(int itemId, int skinId, int userId)
        {
            var item = await GetItem(itemId);
            var homeUser = await _userService.GetUserById(userId.ToString());
            if (item != null && item.Item.OwnerId == homeUser.Id)
            {
                if(item.Definition.Type == "notes")
                {
                    item.Item.Skin = SkinIdToString.ConvertNote(skinId);
                } else
                {
                    item.Item.Skin = SkinIdToString.Convert(skinId);
                }
                _context.HomesItems.Update(item.Item);
                await _context.SaveChangesAsync();
            }

            // Get widget data
            item.WidgetData =  await GetWidgetData(homeUser.Id);
            return item;
        }

        public async Task<ItemViewModel> SelectSong(int itemId, int songId, int userId)
        {
            var item = await _context.HomesItems.Where(s => s.Id == itemId && s.OwnerId == userId).FirstOrDefaultAsync();
            var homeUser = await _userService.GetUserById(userId.ToString());
            var song = await _traxService.GetSingleSongById(songId);
            if (item != null && song != null)
            {
                item.Data = song.Id.ToString();
            } else if(item != null && song == null)
            {
                item.Data = null;
            }

            // Save to db 
            _context.HomesItems.Update(item);
            await _context.SaveChangesAsync();

            // Get widget data
            var dbItem = await GetItem(itemId, true);
            dbItem.WidgetData = await GetWidgetData(homeUser.Id);
            return dbItem;
        }

        public async Task<ItemViewModel> GetItem(int id, bool enableEditing = false)
        {
            var item = await _context.HomesItems.Where(s => s.Id == id).FirstOrDefaultAsync();
            if (item != null)
            {
                var itemData = await _context.HomesItemData.Where(s => s.Id == item.ItemId).FirstOrDefaultAsync();
                if(itemData != null)
                {
                    var itemDataWithDefinition = new ItemViewModel { Definition = itemData, Item = item, EnableEditing = enableEditing };
                    return itemDataWithDefinition;
                }

            }
            return null;
        }

        public async Task<ItemWidgetDataModel> GetWidgetData(int userId)
        {
            return new ItemWidgetDataModel
            {
                User = await _userService.GetUserById(userId.ToString()),
                Rooms = await _roomService.GetRoomsByOwner(userId),
                SongList = await _traxService.GetSongsByOwner(userId),
                Ratings = await GetRatings(userId),
                Badges = await _context.UsersBadges.Where(s => s.UserId == userId).ToListAsync(),
                Guestbook = await GetGuestbookForUser(userId),
                Friends = await _friendService.GetFriendsWithUserData(userId)
            };
        }

        public async Task<ItemViewModel> Rate(int rating, int itemId, int userId)
        {
            // Get item to find home id
            var item = await GetItem(itemId);

            var existingVote = await _context.HomesRating.Where(s => s.HomeId == item.Item.HomeId && s.UserId == userId).FirstOrDefaultAsync();
            if (item != null && existingVote == null)
            {
                if(item.Item.OwnerId != userId)
                {
                    // Lets add the vote if the user hasnt voted before.
                    if(_context.HomesRating.Where(s=>s.HomeId == item.Item.HomeId && s.UserId == userId).Count() == 0)
                    {
                        _context.HomesRating.Add(new HomesRating { UserId = userId, HomeId = item.Item.HomeId, Rating = rating });
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // We gonna need the widget data to display the voting result
            item.WidgetData = await GetWidgetData(item.Item.OwnerId);
            return item;
        }

        public async Task<ItemViewModel> ResetRating(int itemId, int userId)
        {
            // Get item to find home id
            var item = await GetItem(itemId);
            if (item != null)
            {
                if (item.Item.OwnerId == userId)
                {
                    // Lets add the vote if the user hasnt voted before.
                    _context.HomesRating.RemoveRange(await _context.HomesRating.Where(s => s.HomeId == item.Item.HomeId).ToListAsync());
                    await _context.SaveChangesAsync();
                }
            }
            // We gonna need the widget data to display the voting result
            item.WidgetData = await GetWidgetData(item.Item.OwnerId);
            return item;
        }

        public async Task<List<HomesRating>> GetRatings(int userId)
        {
            // Lets figure out the home that the userId owns 
            var home = await _context.Homes.Where(s => s.UserId == userId).FirstOrDefaultAsync();
            if (home == null) return new List<HomesRating>();
            return await _context.HomesRating.Where(s=>s.HomeId == home.Id).ToListAsync();
        }

        public async Task<GuestbookEntry> AddGuestbookEntry(int homeId, string message, int userId)
        {
            var home = await _context.Homes.Where(s => s.Id == homeId && s.UserId == userId).FirstOrDefaultAsync();
            var user = await _userService.GetUserById(userId.ToString());
            if(home != null && user != null)
            {
                var entry = new HomesGuestbook { UserId = userId, HomeId = homeId, Message = message, Timestamp = DateTime.Now };
                _context.HomesGuestbook.Add(entry);
                await _context.SaveChangesAsync();
                
                return new GuestbookEntry { Entry = entry, User = user };
            }
            return null;
        }

        public async Task<HomesGuestbook> DeleteGuestbookEntry(int id, int userId)
        {
            var guestbookEntry = await _context.HomesGuestbook.Where(s=>s.Id == id && s.UserId == userId).FirstOrDefaultAsync(); 
            if(guestbookEntry != null)
            {
                _context.HomesGuestbook.Remove(guestbookEntry);
                await _context.SaveChangesAsync();
            }
            return guestbookEntry;
        }

        public async Task<List<GuestbookEntry>> GetGuestbook(int homeId)
        {
            var entries = new List<GuestbookEntry>();
            var dbEntries = await _context.HomesGuestbook.Where(s => s.HomeId == homeId).ToListAsync();
            foreach(var entry in dbEntries)
            {
                var user = await _userService.GetUserById(entry.UserId.ToString());
                if(user != null)
                {
                    entries.Add(new GuestbookEntry { Entry = entry, User = user });
                }
            }
            return entries.OrderByDescending(s=>s.Entry.Timestamp).ToList();
        }

        public async Task<List<GuestbookEntry>> GetGuestbookForUser(int userId)
        {
            var home = await _context.Homes.Where(s => s.UserId == userId).FirstOrDefaultAsync();
            if(home != null)
            {
                return await GetGuestbook(home.Id);
            }
            return new List<GuestbookEntry>();

        }

        public async Task<ItemViewModel> ConfigureGuestbook(int widgetId)
        {
            var item = await GetItem(widgetId);
            if (item != null)
            {
                if(item.Item.Data == "private")
                {
                    item.Item.Data = "";
                } else
                {
                    item.Item.Data = "private";
                }
                _context.HomesItems.Update(item.Item);
                await _context.SaveChangesAsync();
            }
            return item;
        }


        public async Task<ItemViewModel> PlaceNote(int skin, string text, int userId)
        {
            var inventory = (await GetInventory("notes", userId)).FirstOrDefault();
            var homeForUser = await _context.Homes.Where(s => s.UserId == userId).FirstOrDefaultAsync();
            HomesItems newItem = null;
            if (inventory != null && inventory.Details.Amount > 0 && homeForUser != null)
            {
                // Ok we ensured the user has bought notes and have some left
                // Now lets check if they have a home
                if (homeForUser != null) {

                    // Lets remove or take an item for the user
                    if (inventory.Details.Amount > 1)
                    {
                        inventory.Details.Amount--;
                        _context.HomesInventory.Update(inventory.Details);
                    }
                    else
                    {
                        _context.HomesInventory.Remove(inventory.Details);
                    }
                    // Add the new item to view
                    newItem = new HomesItems { HomeId = homeForUser.Id, OwnerId = userId, ItemId = inventory.Definition.Id, X = 0, Z = 0, Y = 0, Skin = SkinIdToString.ConvertNote(skin), Data = text };
                    _context.HomesItems.Add(newItem);
                    await _context.SaveChangesAsync();
                }
                
            }
            return new ItemViewModel { Item = newItem, Definition = await GetItemDetail(newItem.ItemId), EnableEditing = true, WidgetData = await GetWidgetData(homeForUser.UserId) };
        }

        public async Task<Homes> InitHome(int userId)
        {
            var home = await _context.Homes.Where(s => s.UserId == userId).FirstOrDefaultAsync();
            if(home == null)
            {
                var newHome = new Homes { UserId = userId, AllowDisplay = true, Background = "b_bg_pattern_abstract2", Type = "user" };
                _context.Homes.Add(newHome);
                await _context.SaveChangesAsync();
                // Default items

                // Default items 
                var needle = await GetItemDataByClass("s_needle_3");
                var duck = await GetItemDataByClass("s_sticker_spaceduck");
                var clip = await GetItemDataByClass("s_paper_clip_1");

                _context.HomesItems.Add(new HomesItems { HomeId = newHome.Id, ItemId = needle.Id, OwnerId = userId, Skin = "w_skin_defaultskin", X = 106, Y = 31, Z = 564 });
                _context.HomesItems.Add(new HomesItems { HomeId = newHome.Id, ItemId = duck.Id, OwnerId = userId, Skin = "w_skin_defaultskin", X = 257, Y = 340, Z = 582 });
                _context.HomesItems.Add(new HomesItems { HomeId = newHome.Id, ItemId = clip.Id, OwnerId = userId, Skin = "w_skin_defaultskin", X = 167, Y = 411, Z = 576 });

                // Default notes
                var noteItem = await GetItemDataByClass("commodity_stickienote");
                _context.HomesItems.Add(new HomesItems { HomeId = newHome.Id, ItemId = noteItem.Id, OwnerId = userId, Skin = "n_skin_noteitskin", X = 124, Y = 49, Z = 550, Data = DbRes.T("default_note1", "habbohome") });
                _context.HomesItems.Add(new HomesItems { HomeId = newHome.Id, ItemId = noteItem.Id, OwnerId = userId, Skin = "n_skin_speechbubbleskin", X = 31, Y = 247, Z = 568, Data = DbRes.T("default_note2", "habbohome") });
                _context.HomesItems.Add(new HomesItems { HomeId = newHome.Id, ItemId = noteItem.Id, OwnerId = userId, Skin = "n_skin_notepadskin", X = 115, Y = 447, Z = 570, Data = DbRes.T("default_note3", "habbohome") });

                // Default Widgets
                var defaultWidgets = await _context.HomesItemData.Where(s => s.CssClass == "w_roomswidget" ||
                s.CssClass == "w_roomswidget" ||
                s.CssClass == "w_ratingwidget" ||
                s.CssClass == "w_highscoreswidget" ||
                s.CssClass == "w_guestbookwidget" ||
                s.CssClass == "w_friendswidget" ||
                s.CssClass == "w_badgeswidget" ||
                s.CssClass == "w_traxplayerwidget" ||
                s.CssClass == "w_profilewidget").ToListAsync();

                foreach(var widget in defaultWidgets)
                {
                    if(widget.CssClass == "w_profilewidget")
                    {
                        _context.HomesItems.Add(new HomesItems { HomeId = newHome.Id, ItemId = widget.Id, OwnerId = userId, Skin = "w_skin_defaultskin", X = 445, Y = 28, Z = 546 });
                    } else if (widget.CssClass == "w_roomswidget")
                    {
                        _context.HomesItems.Add(new HomesItems { HomeId = newHome.Id, ItemId = widget.Id, OwnerId = userId, Skin = "w_skin_defaultskin", X = 428, Y = 291, Z = 558 });
                    } else
                    {
                        _context.HomesInventory.Add(new HomesInventory { Amount = 1, ItemId = widget.Id, UserId = userId });
                    }
                }

                await _context.SaveChangesAsync();
                return newHome;
            }
            return home;
        }

        public async Task<HomesItemData> GetItemDataByClass(string className)
        {
            return await _context.HomesItemData.Where(s => s.CssClass == className).FirstOrDefaultAsync();
        }
    }

}
