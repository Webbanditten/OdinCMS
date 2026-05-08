using System;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace KeplerCMS.Services.Implementations
{
    public class FuseService : IFuseService
    {
        private readonly DataContext _context;

        public FuseService(DataContext context)
        {
            _context = context;
        } 

        public async Task<IEnumerable<Fuses>> GetFuses()
        {
            return await _context.Fuses.ToListAsync();
        }
        public async Task<IEnumerable<RankRights>> GetFusesByRank(int rank, bool hasClub)
        {
            var habboClubQuery = (hasClub) ? " OR user_group = 'HABBO_CLUB'" : "";
            return await _context.RankRights.FromSqlRaw("SELECT fuse, rank_id FROM rank_rights where rank_id = @id UNION SELECT fuse, @id as rank_id FROM fuses where user_group = 'ANYONE'" + habboClubQuery, new MySqlParameter ("@id", rank)).IgnoreAutoIncludes().ToListAsync();
        }

        public async Task<IEnumerable<RankRights>> GetRankRights()
        {
            return await _context.RankRights.ToListAsync();
        }

        public async Task<IEnumerable<Rank>>GetRanks()
        {
            return await _context.Ranks.ToListAsync();
        }

        public async Task<Rank> CreateRank(Rank rank, IEnumerable<RankRights> rankRights)
        {
            _context.Ranks.Add(rank);
            await _context.SaveChangesAsync();
            foreach (var rankRight in rankRights)
            {
                rankRight.RankId = rank.Id;
                _context.RankRights.Add(rankRight);
            }
            await _context.SaveChangesAsync();
            return rank;
        }

        public async Task<Rank> UpdateRank(Rank rank, IEnumerable<RankRights> rankRights)
        {
            var dbRank = await _context.Ranks.FirstOrDefaultAsync(s => s.Id == rank.Id);
            
            dbRank.Name = rank.Name;
            _context.Ranks.Update(dbRank);
            await _context.SaveChangesAsync();

            var rankRightsToDelete = new List<RankRights>();
            foreach (var rankRight in await _context.RankRights.Where(s =>
                         s.RankId == rank.Id).ToListAsync())
            {
                if(!rankRights.Any(r => r.FuseName == rankRight.FuseName))
                {
                    rankRightsToDelete.Add(rankRight);
                }
            }
            _context.RankRights.RemoveRange(rankRightsToDelete);
            await _context.SaveChangesAsync();
            
            foreach (var rr in rankRights)
            {
                if (!dbRank.RankRights.Any(r => r.FuseName == rr.FuseName))
                {
                    _context.RankRights.Add(rr);
                }
            }
            await _context.SaveChangesAsync();
            
            return rank;
        }
        public async Task<bool> DeleteRankRights(int rankId)
        {
            var rankRights = await _context.RankRights.Where(r => r.RankId == rankId).ToListAsync();
            _context.RankRights.RemoveRange(rankRights);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRank(int rankId)
        {
            var rank = await _context.Ranks.FirstOrDefaultAsync(r => r.Id == rankId);
            if (rank == null)
                return false;
            _context.Ranks.Remove(rank);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<Rank> GetRankById(int rankId) {
            return await _context.Ranks.FirstOrDefaultAsync(r => r.Id == rankId);
        }

        public async Task<IEnumerable<RankRights>> GetRankRightsByRankId(int id)
        {
            return await _context.RankRights.Where(r => r.RankId == id).ToListAsync();
        }

        public async Task<IEnumerable<Rank>> GetRanksWithUsers()
        {
            var ranks = await GetRanks();
            foreach (var rank in ranks)
            {
                rank.Users = await _context.Users.Where(user => user.Rank == rank.Id).ToListAsync();
            }
            return ranks;
        }

        public async Task<IEnumerable<RankBadges>> GetRankBadges(int rankId)
        {
            return await _context.RankBadges.Where(s=>s.Rank == rankId).ToListAsync();
        }
        public async Task<RankBadges> AddRankBadge(RankBadges rankBadge)
        {
            _context.RankBadges.Add(rankBadge);
            await _context.SaveChangesAsync();
            return rankBadge;
        }
        public async Task<bool> RemoveRankBadge(RankBadges rankBadge)
        {
            _context.RankBadges.Remove(rankBadge);
            await _context.SaveChangesAsync();
            return true;
        }
        
    }

}
