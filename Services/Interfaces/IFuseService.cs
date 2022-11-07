using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IFuseService
    {
        Task<IEnumerable<string>> GetFusesByRank(int rank);
        Task<IEnumerable<NewFuses>> GetFuses();
        Task<IEnumerable<Rank>> GetRanks();
        Task<Rank> GetRankById(int rankId);
        Task<IEnumerable<RankRight>> GetRankRights();
        Task<Rank> CreateRank(Rank rank, IEnumerable<RankRight> rankRights);
        Task<Rank> UpdateRank(Rank rank, IEnumerable<RankRight> rankRights);
        Task<bool> DeleteRankRights(int rankId);
        Task<bool> DeleteRank(int rankId);
        Task<IEnumerable<RankRight>> GetRankRightsByRankId(int id);
    }
}
