using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services.Implementations
{
    public class HighscoreService : IHighscoreService
    {
        private readonly DataContext _context;

        public HighscoreService(DataContext context)
        {
            _context = context;
        }

        /// <summary>The games with a running total on the user (users.battleball_points / snowstorm_points).</summary>
        private static bool HasRunningTotal(string gameType) => gameType == "battleball" || gameType == "snowstorm";

        public async Task<List<HighscoreEntry>> GetTop(string gameType, HighscorePeriod period, int limit)
        {
            if (period == HighscorePeriod.AllTime && HasRunningTotal(gameType))
            {
                var users = gameType == "battleball"
                    ? _context.Users.Where(u => u.BattleballPoints > 0).OrderByDescending(u => u.BattleballPoints)
                    : _context.Users.Where(u => u.SnowStormPoints > 0).OrderByDescending(u => u.SnowStormPoints);
                return await users
                    .Take(limit)
                    .Select(u => new HighscoreEntry
                    {
                        UserId = u.Id,
                        Username = u.Username,
                        Figure = u.Figure,
                        Score = gameType == "battleball" ? u.BattleballPoints : u.SnowStormPoints,
                    })
                    .ToListAsync();
            }

            // Games without a running total (Wobble Squabble) add up all their highscores rows for all time.
            var today = DateTime.Today;
            var from = period switch
            {
                HighscorePeriod.Month => new DateTime(today.Year, today.Month, 1),
                HighscorePeriod.AllTime => DateTime.MinValue,
                _ => today.AddDays(-(((int)today.DayOfWeek + 6) % 7)), // Monday
            };

            var totals = await _context.Highscores
                .Where(h => h.GameType == gameType && h.Date >= from)
                .GroupBy(h => h.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Score = g.Sum(h => h.Score),
                    Games = g.Count(),
                    Wins = g.Sum(h => h.Won == true ? 1 : 0),
                })
                .Where(t => t.Score > 0)
                .OrderByDescending(t => t.Score)
                .Take(limit)
                .ToListAsync();

            var ids = totals.Select(t => t.UserId).ToList();
            var users2 = await _context.Users
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.Username, u.Figure })
                .ToDictionaryAsync(u => u.Id);

            return totals
                .Where(t => users2.ContainsKey(t.UserId))
                .Select(t => new HighscoreEntry
                {
                    UserId = t.UserId,
                    Username = users2[t.UserId].Username,
                    Figure = users2[t.UserId].Figure,
                    Score = t.Score,
                    Games = t.Games,
                    Wins = t.Wins,
                })
                .ToList();
        }
    }
}
