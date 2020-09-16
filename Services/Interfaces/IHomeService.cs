using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IHomeService
    {
        Task<bool> Save(int homeId, int currentUserId, SaveModel data);
        Task<List<HomesCategories>> GetStoreCategories();
        Task<List<CatalogItem>> GetStoreCatelog(int category, int subCategory);
        Task<HomesInventory> RemoveItem(int homeId, int itemId, int userId);
        Task<ItemViewModel> PlaceItem(int homeId, int userId, int z, int inventoryId, string data = null);
        Task<ItemViewModel> EditItem(int homeId, int itemId, int skinId, int userId);
        Task<ItemViewModel> GetItem(int id, bool enableEditing = false);
        Task<List<InventoryItem>> GetInventory(int homeId, string type, int userId);

        Task<HomeViewModel> GetHome(int userId, bool enableEditing, int? currentUserId = null);
        Task<HomeViewModel> GetHomeByGroupName(string groupname, bool enableEditing, int? currentUserId = null);
        Task<HomeViewModel> GetHomeByGroupId(int groupId, bool enableEditing, int? currentUserId = null);
        Task<Homes> GetHomeDetailsById(int homeId);
        Task<HomesItemData> GetItemDetail(int id);
        Task<List<CatalogItem>> GetCatelogItemsInCategory(int category);
        Task <CatalogItem> GetProduct(int productId);
        Task<bool> GiveItem(int itemId, int amount, int userId);
        Task<InventoryItem> GetInventoryItem(int inventoryId, int userId);

        Task<ItemWidgetDataModel> GetWidgetData(int homeId, int? userId);
        Task<ItemViewModel> SelectSong(int homeId, int itemId, int songId, int userId);

        Task<ItemViewModel> Rate(int homeId, int rating, int itemId, int userId);
        Task<List<HomesRating>> GetRatings(int homeId);

        Task<GuestbookEntry> AddGuestbookEntry(int homeid, string message, int userId);
        Task<HomesGuestbook> DeleteGuestbookEntry(int id, int userId);
        Task<List<GuestbookEntry>> GetGuestbook(int homeId);
        Task<ItemViewModel> ConfigureGuestbook(int widgetId);
        Task<ItemViewModel> ResetRating(int homeid, int widgetid, int userId);
        Task<ItemViewModel> PlaceNote(int homeid, int skin, string text, int userId);
        Task<Homes> InitHome(int userId);
        Task<Homes> InitGroup(string name, string badge, string description, int userId);
        Task<HomesItemData> GetItemDataByClass(string className);

        Task<bool> CanEditHome(int homeId, int? userId);

        Task<List<GroupViewModel>> GetGroupsForUser(int userId);
        Task<Homes> UpdateGroupBadge(int homeId, string code, int userId);
        Task<List<GroupViewModel>> GetGroupMembers(Homes home);
        Task<List<GroupMembers>> GetGroupMembers(int groupId);
        Task<Rooms> GetGroupRoom(Homes home);

        Task<bool> IsGroupUrlValid(string url);
        Task<Homes> UpdateGroup(int groupId, string name, string description, string url, int type, int? roomId);

        Task<bool> DeleteGroup(int groupId);

        Task<GroupMembers> GetMembership(int groupId, int userId);

        Task<List<GroupMembers>> AcceptMembers(int groupId, int[] targetIds);
        Task<List<GroupMembers>> GiveGroupRights(int groupId, int[] targetIds);
        Task<List<GroupMembers>> RemoveGroupRights(int groupId, int[] targetIds);
        Task<List<GroupMembers>> RemoveMembers(int groupId, int[] targetIds);
    }
}
