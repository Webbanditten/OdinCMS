using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface IHighscoreService
    {
        /// <summary>
        /// Top players for a game: the sum of their game scores this week (from Monday) or this month,
        /// or their all-time running total.
        /// </summary>
        Task<List<HighscoreEntry>> GetTop(string gameType, HighscorePeriod period, int limit);
    }
}
