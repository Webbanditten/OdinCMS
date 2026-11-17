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
    }
}
