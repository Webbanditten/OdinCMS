using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IRewardService
    {
        Task<List<Rewards>> GetRewardsBetweenDates(DateTime from, DateTime to);
    }
}
