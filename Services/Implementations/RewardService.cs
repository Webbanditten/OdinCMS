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
            foreach (var reward in rewards)
            {
                if (!string.IsNullOrEmpty(reward.ItemDefinitions))
                {
                    string[] itemDefinitions;
                    if (reward.ItemDefinitions.Contains(","))
                    {
                        itemDefinitions = reward.ItemDefinitions.Split(",");
                    }
                    else
                    {
                        itemDefinitions = new string[] { reward.ItemDefinitions };
                    }

                    reward.ItemsDefinitions = new List<ItemsDefinitions>();
                    foreach (var itemDefinition in itemDefinitions)
                    {
                        var itemDefinitionInt = int.TryParse(itemDefinition, out int itemDefinitionIntResult) ? itemDefinitionIntResult : 0;
                        var dbDefinition = _context.ItemsDefinitions.Where(s => s.Id == itemDefinitionInt).FirstOrDefault();
                        if (dbDefinition != null)
                        {
                            reward.ItemsDefinitions.Add(dbDefinition);
                        }
                    }
                }
            }
            return rewards;
        }
    }
}
