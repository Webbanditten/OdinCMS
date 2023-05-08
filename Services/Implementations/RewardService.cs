using Isopoh.Cryptography.Argon2;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class RewardService : IRewardService
    {
        private readonly DataContext _context;
        public RewardService(DataContext context, IUserService userService)
        {
            _context = context;
        }
        public async Task<List<Rewards>> GetRewardsBetweenDates(DateTime from, DateTime to)
        {
            var rewards = await _context.Rewards.Where(s => s.AvailableFrom >= from && s.AvailableTo <= to).ToListAsync();

            var itemDefinitionIds = rewards
                .Where(r => !string.IsNullOrEmpty(r.ItemDefinitions))
                .SelectMany(r => r.ItemDefinitions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s, out int itemDefinitionIntResult) ? itemDefinitionIntResult : 0))
                .Where(id => id != 0)
                .Distinct()
                .ToList();

            var dbDefinitions = await _context.ItemsDefinitions
                .Where(s => itemDefinitionIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            foreach (var reward in rewards)
            {
                if (!string.IsNullOrEmpty(reward.ItemDefinitions))
                {
                    var itemDefinitions = reward.ItemDefinitions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out int itemDefinitionIntResult) ? itemDefinitionIntResult : 0)
                        .Where(id => id != 0)
                        .ToList();

                    reward.ItemsDefinitions = itemDefinitions
                        .Where(id => dbDefinitions.ContainsKey(id))
                        .Select(id => dbDefinitions[id])
                        .ToList();
                }
            }

            return rewards;
        }

        public async Task<List<Rewards>> GetPreviousRewards()
        {
            return await GetRewardsBetweenDates(DateTime.Now.AddYears(-100), DateTime.Now);
        }

        public async Task<List<Rewards>> GetCurrentRewards()
        {
            return await GetRewardsBetweenDates(DateTime.Now, DateTime.Now.AddYears(100));
        }
        
        public async Task<List<Rewards>> GetRewards()
        {
            return await _context.Rewards.ToListAsync();
        }
    }
}
