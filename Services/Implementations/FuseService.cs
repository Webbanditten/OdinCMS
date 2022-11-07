using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class FuseService : IFuseService
    {
        private readonly DataContext _context;

        public FuseService(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NewFuses>> GetFuses()
        {
            return await _context.NewFuses.ToListAsync();
        }

        public async Task<IEnumerable<string>> GetFusesByRank(int rank)
        {
            return await _context.Fuses.Where(f => f.MinRank <= rank).Select(s=>s.Fuse.ToLower()).ToListAsync();
        }

        public async Task<IEnumerable<RankRight>> GetRankRights()
        {
            return await _context.RankRights.ToListAsync();
        }

        public async Task<IEnumerable<Rank>>GetRanks()
        {
            return await _context.Ranks.ToListAsync();
        }

        public async Task<Rank> CreateRank(Rank rank, IEnumerable<RankRight> rankRights)
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

        public async Task<Rank> UpdateRank(Rank rank, IEnumerable<RankRight> rankRights)
        {
            _context.Ranks.Update(rank);
            await _context.SaveChangesAsync();
            await this.DeleteRankRights(rank.Id);
            foreach (var rankRight in rankRights)
            {
                rankRight.RankId = rank.Id;
                _context.RankRights.Add(rankRight);
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

            await this.DeleteRankRights(rankId);

            return true;
        }

        public async Task<Rank> GetRankById(int rankId) {
            return await _context.Ranks.FirstOrDefaultAsync(r => r.Id == rankId);
        }

        public async Task<IEnumerable<RankRight>> GetRankRightsByRankId(int id)
        {
            return await _context.RankRights.Where(r => r.RankId == id).ToListAsync();
        }
    }

}
