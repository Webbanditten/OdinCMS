using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    /// <summary>
    /// Habbo Homes (user and group pages): items, inventory, store, guestbook, ratings and the group data the
    /// home widgets show. Methods that change a home take the home being edited and the acting user, and act only
    /// on items that belong to that home; the caller checks <see cref="CanEditHome"/> first.
    /// </summary>
    public interface IHomeService
    {
        // Viewing
        Task<HomeViewModel> GetHome(int userId, bool enableEditing, int? currentUserId = null);
        Task<HomeViewModel> GetHomeByGroupName(string groupname, bool enableEditing, int? currentUserId = null);
        Task<HomeViewModel> GetHomeByGroupId(int groupId, bool enableEditing, int? currentUserId = null);
        Task<Homes> GetHomeDetailsById(int homeId);
        Task<ItemViewModel> GetItem(int id, bool enableEditing = false);
        Task<ItemWidgetDataModel> GetWidgetData(int homeId, int? userId);
        Task<bool> CanEditHome(int homeId, int? userId);

        // Editing a home (homeId = the home being edited)
        Task<bool> Save(int homeId, int currentUserId, SaveModel data);
        /// <summary>Places a sticker or widget (<paramref name="type"/>) from inventory. A group's widgets come from the group's stacks.</summary>
        Task<ItemViewModel> PlaceItem(int homeId, int inventoryId, int z, int userId, string type);
        Task<ItemViewModel> PlaceNote(int homeid, int skin, string text, int userId);
        Task<HomesInventory> RemoveItem(int homeId, int itemId, int userId);
        Task<ItemViewModel> EditItem(int homeId, int itemId, int skinId, int userId);
        Task<ItemViewModel> SelectSong(int homeId, int itemId, int songId, int userId);
        Task<List<InventoryItem>> GetInventory(int homeId, string type, int userId);
        Task<InventoryItem> GetInventoryItem(int inventoryId, int userId);

        // Store
        Task<List<HomesCategories>> GetStoreCategories();
        /// <summary>The store's category list for one item type: each main category's sub categories, or itself without any.</summary>
        Task<List<HomesCategories>> GetStoreCategoryList(string type);
        /// <summary>The store product that sells notes (bought from the note editor).</summary>
        Task<CatalogItem> GetNotesProduct();
        /// <summary>How many notes the user has left to place.</summary>
        Task<int> GetNoteCount(int userId);
        Task<List<CatalogItem>> GetStoreCatelog(int category, int subCategory);
        Task<List<CatalogItem>> GetCatelogItemsInCategory(int category);
        Task<CatalogItem> GetProduct(int productId);
        Task<HomesItemData> GetItemDetail(int id);
        Task<HomesItemData> GetItemDataByClass(string className);
        /// <summary>Whether the user may receive this item (a background can be owned only once).</summary>
        Task<bool> CanReceiveItem(int itemId, int userId);
        Task<bool> GiveItem(int itemId, int amount, int userId);

        // Widgets anyone can use
        Task<ItemViewModel> Rate(int rating, int itemId, int userId);
        Task<ItemViewModel> ResetRating(int homeid, int widgetid, int userId);
        Task<List<HomesRating>> GetRatings(int homeId);
        Task<GuestbookEntry> AddGuestbookEntry(int homeid, string message, int userId);
        Task<HomesGuestbook> DeleteGuestbookEntry(int id, int userId);
        Task<List<GuestbookEntry>> GetGuestbook(int homeId);
        Task<ItemViewModel> ConfigureGuestbook(int widgetId, int userId);

        // Creating homes and groups
        Task<Homes> InitHome(int userId);
        Task<Homes> InitGroup(string name, string description, int userId);

        // Groups
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
        Task<GroupMembers> AddPendingMember(int groupId, int userId);
        Task<List<Homes>> GetRecentGroups();
        Task<List<Homes>> GetHotelGroups();
        Task<List<Homes>> GetActiveGroups();
    }
}
