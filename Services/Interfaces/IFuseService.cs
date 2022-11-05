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
        Task<IEnumerable<RankRight>> GetRankRights();
    }
}
