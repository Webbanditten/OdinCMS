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

        public async Task<List<RewardsViewModel>> GetRewardsBetweenDates(DateTime from, DateTime to, int userId)
        {
            
            var rewards = await _context.Rewards.Where(s => s.AvailableFrom >= from && s.AvailableTo <= to).Select(reward => new RewardsViewModel
            {
                Id = reward.Id,
                RequiredStreak = reward.RequiredStreak,
                Description = reward.Description,
                AvailableFrom = reward.AvailableFrom,
                AvailableTo = reward.AvailableTo,
                ItemDefinitions = reward.ItemDefinitions,
                Redeemed = false,
            }).ToListAsync();
            
            foreach (var reward in rewards)
            {
                var redeemed = await _context.RewardsRedeemed
                    .FirstOrDefaultAsync(s => s.RewardId == reward.Id && s.UserId == userId);

                reward.Redeemed = redeemed != null;
            }

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

            return rewards.OrderBy(s=>s.AvailableFrom).ToList();
        }

        public async Task<List<Rewards>> GetPreviousRewards()
        {
            var today = DateTime.Now;
            var year = today.Year;
            var month = today.Month;
            var day = today.Day;
            return (await GetRewardsBetweenDates(DateTime.Now.AddYears(-100), new DateTime(year, month, day, 00,00,00))).OrderByDescending(s=>s.AvailableFrom).ToList();
        }

        public async Task<List<Rewards>> GetCurrentRewards()
        {
            var today = DateTime.Now;
            var year = today.Year;
            var month = today.Month;
            var day = today.Day;
            return (await GetRewardsBetweenDates(new DateTime(year, month, day, 00,00,00), DateTime.Now.AddYears(100))).OrderByDescending(s=>s.AvailableFrom).ToList();
        }

        public async Task<Rewards> CreateReward(Rewards reward)
        {
            _context.Rewards.Add(reward);
            await _context.SaveChangesAsync();
            
            return reward;
        }

        public async Task<Rewards> UpdateReward(Rewards reward)
        {
            _context.Rewards.Update(reward);
            await _context.SaveChangesAsync();
            return reward;
        }

        public async Task<Rewards> DeleteReward(int id)
        {
            _context.Rewards.Remove(new Rewards { Id = id });
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<List<Rewards>> GetRewards()
        {
            return await _context.Rewards.ToListAsync();
        }
        
        
        
    }
}
