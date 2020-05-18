using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IFriendService
    {
        Task<List<Friends>> GetFriends(int userId);

        Task<List<FriendsViewModel>> GetFriendsWithUserData(int userId);

        Task<FriendRequests> AddFriend(int to, int from);
        Task<bool> RequestExists(int from, int to);
    }
}
