using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface IRewardCalendarService
    {
        Task<List<RewardPoolView>> GetPools();
        Task<RewardPool> CreatePool(string name, string description);
        Task UpdatePool(int id, string name, string description);
        Task DeletePool(int id);
        Task AddPoolItems(int poolId, IEnumerable<int> definitionIds);
        Task RemovePoolItem(int poolId, int definitionId);
        Task ConfirmPoolItem(int poolId, int definitionId);
        Task MovePoolItem(int poolId, int definitionId, int direction);

        Task<List<RewardTemplate>> GetTemplates();
        RewardPlan ReadPlan(RewardTemplate template);
        Task<RewardTemplate> SaveTemplate(string name, RewardPlan plan);
        Task DeleteTemplate(int id);

        Task<List<RewardPlanDay>> Generate(RewardPlan plan, DateTime start);
        Task<int> CreateCalendar(IEnumerable<RewardPlanDay> days, string note, string label);
        Task<int> CopyPeriod(DateTime from, DateTime to, DateTime target, string label);

        Task<List<RewardEntry>> GetRewards(DateTime from, DateTime to);
        Task<List<RewardFurni>> GetFurni(IEnumerable<int> ids);
        Task<Rewards> SaveReward(int id, DateTime from, DateTime to, IEnumerable<int> items, string badge, string note, string label);
        Task DeleteReward(int id);
        Task<int> DeleteUpcoming(string label);
        Task<List<string>> GetLabels();
    }
}
