using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KeplerCMS.Models
{
    public enum HighscorePeriod
    {
        Week,
        Month,
        AllTime,
    }

    /// <summary>
    /// A highscore table placed in page or news text with a code:
    ///   {highscores_battleball}            top 10 this week
    ///   {highscores_snowstorm_month}       top 10 this month
    ///   {highscores_battleball_all}        top 10 all time (the players' running totals)
    ///   {champions_battleball}             top 3 this month
    ///   {highscores_snowstorm_week_5}      any of the above with a row count (1-50)
    /// "snowball" is accepted for SnowStorm.
    /// </summary>
    public class HighscoreWidget
    {
        public static readonly Regex Code = new Regex(
            @"\{(highscores|champions)_(battleball|snowstorm|snowball)(?:_(week|month|all|alltime))?(?:_(\d{1,2}))?\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>"battleball" or "snowstorm", as Odin stores it.</summary>
        public string GameType { get; set; }
        public HighscorePeriod Period { get; set; }
        public int Limit { get; set; }

        public static HighscoreWidget FromCode(Match match)
        {
            var champions = match.Groups[1].Value.Equals("champions", StringComparison.OrdinalIgnoreCase);
            var game = match.Groups[2].Value.ToLowerInvariant() == "battleball" ? "battleball" : "snowstorm";
            var period = match.Groups[3].Value.ToLowerInvariant() switch
            {
                "month" => HighscorePeriod.Month,
                "all" or "alltime" => HighscorePeriod.AllTime,
                "week" => HighscorePeriod.Week,
                _ => champions ? HighscorePeriod.Month : HighscorePeriod.Week,
            };
            var limit = int.TryParse(match.Groups[4].Value, out var n) ? Math.Clamp(n, 1, 50) : (champions ? 3 : 10);
            return new HighscoreWidget { GameType = game, Period = period, Limit = limit };
        }
    }

    public class HighscoreEntry
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Figure { get; set; }
        public int Score { get; set; }
        /// <summary>Games counted in the period (0 for the all-time list, which reads the running totals).</summary>
        public int Games { get; set; }
        public int Wins { get; set; }
    }
}
