using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;

namespace KeplerCMS.Services.Interfaces
{
    public interface IRoomChatlogsService
    {
        Task<List<RoomChatlogs>> GetByUserId(int userId, int timestampLimiter, int take, int skip);
        Task<List<RoomChatlogs>> GetByRoomId(int userId, int timestampLimiter, int take, int skip);
    }
}
