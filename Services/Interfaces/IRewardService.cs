using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface IRewardService
    {
        Task<List<Rewards>> GetRewardsBetweenDates(DateTime from, DateTime to);
        Task<List<RewardsViewModel>> GetRewardsBetweenDates(DateTime from, DateTime to, int userId);
        Task<List<Rewards>> GetPreviousRewards();
        Task<List<Rewards>> GetCurrentRewards();
        Task<Rewards> CreateReward(Rewards reward);
        Task<Rewards> UpdateReward(Rewards reward);
        Task<Rewards> DeleteReward(int id);
    }
}
