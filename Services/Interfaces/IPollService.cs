using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface IPollService
    {
        Task<List<Poll>> GetAll();
        Task<Poll> Get(int id);
        Task<Poll> Create(PollCreateViewModel model);
        Task<Poll> Update(int id, PollCreateViewModel model);
        Task<bool> Remove(int id);
        Task<bool> ToggleEnabled(int id);
        Task<PollResultsViewModel> GetResults(int pollId);
    }
}
