using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IFuseService
    {
        Task<IEnumerable<Fuses>> GetFuses();
        Task<IEnumerable<Rank>> GetRanks();
        Task<Rank> GetRankById(int rankId);
        Task<IEnumerable<RankRights>> GetRankRights();
        Task<Rank> CreateRank(Rank rank, IEnumerable<RankRights> rankRights);
        Task<Rank> UpdateRank(Rank rank, IEnumerable<RankRights> rankRights);
        Task<bool> DeleteRankRights(int rankId);
        Task<bool> DeleteRank(int rankId);
        Task<IEnumerable<RankRights>> GetFusesByRank(int rank, bool hasClub);
        Task<IEnumerable<RankRights>> GetRankRightsByRankId(int id);
        Task<IEnumerable<Rank>> GetRanksWithUsers();
        Task<IEnumerable<RankBadges>> GetRankBadges(int rankId);
        Task<RankBadges> AddRankBadge(RankBadges rankBadge);
        Task<bool> RemoveRankBadge(RankBadges rankBadge);
    }
}
