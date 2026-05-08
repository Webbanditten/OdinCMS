using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;

namespace KeplerCMS.Services.Interfaces
{
    public interface IRoomService
    {
        Task<List<Rooms>> GetPublicRooms();
        Task<List<Rooms>> GetRoomsByOwner(int ownerId);
        Task<SearchRoomsModel> Search(string search, int take, int skip);
        Task<List<object>> SearchAll(string search, int take);
        Task<string> UpdateRoom(RoomsUpdateModel model);
        
        Task<Rooms> GetRoom(int id);

    }
}
