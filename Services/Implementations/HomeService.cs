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

namespace KeplerCMS.Services.Implementations
{
    public class HomeService : IHomeService
    {
        private readonly DataContext _context;
        private readonly IUserService _userService;

        public HomeService(DataContext context, IUserService userService)
        {
            _userService = userService;
            _context = context;
        }
        public async Task<HomeViewModel> GetHome(int userId, bool enableEditing)
        {
            // Get home for type user
            var home = await _context.Homes.Where(s => s.UserId == userId).FirstOrDefaultAsync();
            var itemsInHome = await _context.HomesItems.Where(s => s.HomeId == home.Id).ToListAsync();
            var homeUser = await _userService.GetUserById(userId.ToString());
            var homeViewModel = new HomeViewModel { Home = home, Items = new List<ItemViewModel>(), HomeUser = homeUser };

            foreach (var item in itemsInHome)
            {
                var itemData = await _context.HomesItemData.Where(s => s.Id == item.ItemId).FirstOrDefaultAsync();
                var itemDataWithDefinition = new ItemViewModel { Definition = itemData, Item = item, EnableEditing = enableEditing };

                if(itemData.Type == "widgets" && itemData.CssClass == "ProfileWidget")
                {
                    itemDataWithDefinition.WidgetData = new ItemWidgetDataModel { User = homeUser };
                }
                
                homeViewModel.Items.Add(itemDataWithDefinition);
            }
            
            return homeViewModel;
        }

        public async Task<Homes> GetHomeDetailsById(int homeId)
        {
            return await _context.Homes.Where(s => s.Id == homeId).FirstOrDefaultAsync();
        }

        public Task<List<HomesCatalog>> GetInventory(string type)
        {
            throw new NotImplementedException();
        }

        public async Task<List<HomesCategories>> GetStoreCategories()
        {
            return await _context.HomesCategories.ToListAsync(); 
        }

        public async Task<List<CatalogItem>> GetCatelogItemsInCategory(int category)
        {
            var items = new List<CatalogItem>();
            var itemsInCategory = await _context.HomesCatalog.Where(s => s.Category == category).ToListAsync();
            foreach (var i in itemsInCategory)
            {
                items.Add(new CatalogItem { Details = i, Definition = await GetItemDetail(i.ItemId) });
            }
            return items;
        }

        public async Task<List<CatalogItem>> GetStoreCatelog(int categoryId, int subCategoryId)
        {
            var category = await _context.HomesCategories.Where(s => s.Id == categoryId).FirstOrDefaultAsync();
            var subCategory = await _context.HomesCategories.Where(s => s.Id == subCategoryId).FirstOrDefaultAsync();
            if(category != null)
            {
                var items = new List<CatalogItem>();
                if (subCategory != null)
                {
                    items.AddRange(await GetCatelogItemsInCategory(subCategory.Id));
                } else
                {
                    items.AddRange(await GetCatelogItemsInCategory(category.Id));
                }
                return items;
            }
            return null;
        }

        public async Task<PlaceItem> PlaceItem(int inventoryId, int z, int userId, string data = null)
        {
            var item = await _context.HomesInventory.Where(s => s.Id == inventoryId && s.UserId == userId).FirstOrDefaultAsync();
            var homeForUser = await _context.Homes.Where(s => s.UserId == userId).FirstOrDefaultAsync();
            HomesItems newItem = null;
            if(item != null && homeForUser != null)
            {
                if(item.Amount > 1)
                {
                    item.Amount--;
                    _context.HomesInventory.Update(item);
                } else
                {
                    _context.HomesInventory.Remove(item);
                }
                newItem = new HomesItems { HomeId = homeForUser.Id, OwnerId = userId, ItemId = item.ItemId, X = 0, Z = z, Y = 0, Skin = "w_skin_defaultskin" };
                _context.HomesItems.Add(newItem);
                await _context.SaveChangesAsync();
            }

            return new PlaceItem { Details = newItem, Definition = await GetItemDetail(newItem.ItemId)  };
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


        public Task<HomesItems> SaveItem(HomesItems item)
        {
            throw new NotImplementedException();
        }




        public async Task<bool> Save(string userId, SaveModel data)
        {
            var user = await _userService.GetUserById(userId);
            if (user == null) return false;

            var itemsChanged = new List<HomesItems>();
            itemsChanged.AddRange(SaveDataStringConverter.GetItemsFromString(data.Stickers));
            itemsChanged.AddRange(SaveDataStringConverter.GetItemsFromString(data.Widgets));
            itemsChanged.AddRange(SaveDataStringConverter.GetItemsFromString(data.StickieNotes));

            foreach (var item in itemsChanged)
            {
                var dbItem = await _context.HomesItems.Where(s=>s.Id == item.Id).FirstOrDefaultAsync();
                if(dbItem != null)
                {
                    // Lets check if they actually own the item
                    var home = await _context.Homes.Where(s => s.Id == dbItem.HomeId).FirstOrDefaultAsync();
                    if (home != null && home.UserId == user.Id)
                    {
                        dbItem.X = item.X;
                        dbItem.Y = item.Y;
                        dbItem.Z = item.Z;
                        _context.HomesItems.Update(dbItem);
                    }
                }

            }
            await _context.SaveChangesAsync();
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
                similarProductInInventory.Amount++;
                _context.HomesInventory.Update(similarProductInInventory);
            } else
            {
                _context.HomesInventory.Add(new HomesInventory { Amount = amount, UserId = userId, ItemId = itemId });
            }
            await _context.SaveChangesAsync();
            return true;
        }

        async Task<List<InventoryItem>> IHomeService.GetInventory(string type, int userId)
        {
            var items = new List<InventoryItem>();
            var itemsInCategory = await _context.HomesInventory.Where(s=>s.UserId == userId).ToListAsync();
            foreach (var i in itemsInCategory)
            {
                items.Add(new InventoryItem { Details = i, Definition = await GetItemDetail(i.ItemId) });
            }

            return items.Where(s=>s.Definition.Type == type).ToList();
        }

        public async Task<InventoryItem> GetInventoryItem(int inventoryId, int userId)
        {
            InventoryItem invItem = null;
            var dbItem = await _context.HomesInventory.Where(s => s.Id == inventoryId && s.UserId == userId).FirstOrDefaultAsync();
            if(dbItem != null)
            {
                invItem = new InventoryItem { Definition = await GetItemDetail(dbItem.ItemId), Details = dbItem };
            }

            return invItem;
        }
    }

}
