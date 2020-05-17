using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IHomeService
    {
        Task<bool> Save(int currentUserId, SaveModel data);
        Task<List<HomesCategories>> GetStoreCategories();
        Task<List<CatalogItem>> GetStoreCatelog(int category, int subCategory);
        Task<HomesInventory> RemoveItem(int itemId, int userId);
        Task<ItemViewModel> PlaceItem(int userId, int z, int inventoryId, string data = null);
        Task<ItemViewModel> EditItem(int itemId, int skinId, int userId, string data = null);
        Task<ItemViewModel> GetItem(int id, bool enableEditing = false);
        Task<List<InventoryItem>> GetInventory(string type, int userId);

        Task<HomeViewModel> GetHome(int userId, bool enableEditing);
        Task<Homes> GetHomeDetailsById(int homeId);
        Task<HomesItemData> GetItemDetail(int id);
        Task<List<CatalogItem>> GetCatelogItemsInCategory(int category);
        Task <CatalogItem> GetProduct(int productId);
        Task<bool> GiveItem(int itemId, int amount, int userId);
        Task<InventoryItem> GetInventoryItem(int inventoryId, int userId);

        Task<ItemWidgetDataModel> GetWidgetData(int userId);
        Task<ItemViewModel> SelectSong(int itemId, int songId, int userId);

        Task<ItemViewModel> Rate(int rating, int itemId, int userId);
        Task<List<HomesRating>> GetRatings(int userId);


    }
}
