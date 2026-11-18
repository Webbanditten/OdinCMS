using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KeplerCMS.Models
{
    /// <summary>
    /// The codes housekeeping can write into page boxes and news text. The editors' Codes button lists them
    /// (wwwroot/housekeeping/js/editor-codes.js, which reads <see cref="EditorJson"/>).
    /// Box codes are rendered by Views/Shared/Containers/Container.cshtml, highscore codes by <see cref="HighscoreWidget"/>,
    /// furni for sale ({buy_furni_ID}, inserted with the editors' Buy furni button) by <see cref="BuyFurniWidget"/>, rooms
    /// ({room_ID}, the editors' Room button) by <see cref="RoomWidget"/>.
    /// </summary>
    public static class ContentCodes
    {
        public const string Credits = "{credits}";
        public const string Rewards = "{rewards}";
        public const string RewardsMonth = "{rewards_month}";
        public const string LastOnline = "{lastonline}";
        public const string Staff = "{staff}";
        public const string PopularFurni = "{popular_furni}";
        public const string Xmas2006 = "{xmas2006}";
        public const string XmasEcard = "{xmas_ecard}";
        public const string XmasHomeGift = "{xmas_home_gift}";
        public const string HabbowoodIntro = "{habbowood_intro}";
        public const string HabbowoodTop = "{habbowood_top}";
        public const string HabbowoodNew = "{habbowood_new}";

        public record Code(string Value, string Name, string Description);

        /// <summary>
        /// Page boxes only. A box with one of these shows just that widget (its title and theme stay); any other text
        /// in the box is not shown. Checked in this order when a box holds several.
        /// </summary>
        public static readonly IReadOnlyList<Code> BoxCodes = new[]
        {
            new Code(Credits, "Credits", "The visitor's credit balance and the voucher code form (logged out: a log-in hint)."),
            new Code(Xmas2006, "Christmas calendar", "December's rewards as the 2006 advent calendar."),
            new Code(Rewards, "Daily rewards", "Rewards from 15 days back to 15 days ahead, today's highlighted. For a set period write {rewards_2026-11-01_2026-11-30} (both days included)."),
            new Code(RewardsMonth, "Daily rewards (this month)", "This calendar month's rewards only, so the box never shows next month's (an advent calendar's doors)."),
            new Code(LastOnline, "Last online", "The 30 Habbos who logged in most recently."),
            new Code(Staff, "Staff", "Every rank with members, with their avatars."),
            new Code(PopularFurni, "Popular furni", "The furni players bought most in the last 30 days."),
            new Code(XmasEcard, "Christmas card", "The 2006 Christmas card (732×473: use a nobox on a one_wide page) with the sender's cards and their links; logged-in senders can have it e-mailed to their friend."),
            new Code(XmasHomeGift, "Christmas Home gift", "Free Habbo Home decorations (setting cms.xmas_home_gift: item ids), fetched once per player each Christmas while this box is shown."),
            new Code(HabbowoodIntro, "Habbowood intro", "The Habbowood intro animation (534×325) with its Shoot a movie / Watch movies / Meet the filmmakers buttons."),
            new Code(HabbowoodTop, "Habbowood: best rated", "The published Habbowood movies with the best rating (players' 1-5 stars), with their director and votes."),
            new Code(HabbowoodNew, "Habbowood: newest", "The latest published Habbowood movies."),
        };

        /// <summary>{rewards}, {rewards_month} or {rewards_YYYY-MM-DD_YYYY-MM-DD}.</summary>
        private static readonly Regex RewardsCode = new(@"\{rewards(?:_(month)|_(\d{4}-\d{2}-\d{2})_(\d{4}-\d{2}-\d{2}))?\}", RegexOptions.Compiled);

        /// <summary>
        /// The days a rewards box shows (from 00:00 on the first day to the end of the last day), or null when the text
        /// has no rewards code. {rewards}: 15 days back to 15 days ahead; {rewards_month}: this month; a dated code: that
        /// period (the days in either order).
        /// </summary>
        public static (DateTime From, DateTime To)? RewardsPeriod(string text, DateTime now)
        {
            var match = text == null ? null : RewardsCode.Match(text);
            if (match == null || !match.Success) return null;

            if (match.Groups[1].Success)
            {
                var first = new DateTime(now.Year, now.Month, 1);
                return (first, first.AddMonths(1).AddTicks(-1));
            }

            if (match.Groups[2].Success
                && DateTime.TryParseExact(match.Groups[2].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var a)
                && DateTime.TryParseExact(match.Groups[3].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var b))
            {
                var from = a <= b ? a : b;
                var to = a <= b ? b : a;
                return (from, to.AddDays(1).AddTicks(-1));
            }

            return (now.AddDays(-15), now.AddDays(15));
        }

        public record Game(string Id, string Name);

        public static readonly IReadOnlyList<Game> Games = new[]
        {
            new Game("battleball", "Battle Ball"),
            new Game("snowstorm", "SnowStorm"),
            new Game("wobblesquabble", "Wobble Squabble"),
        };

        /// <summary>The ready-made highscore lists; "{0}" is the game id. Any period and row count can be written out.</summary>
        public record HighscoreList(string Format, string Name);

        public static readonly IReadOnlyList<HighscoreList> HighscoreLists = new[]
        {
            new HighscoreList("{{highscores_{0}}}", "Top 10 this week"),
            new HighscoreList("{{highscores_{0}_month}}", "Top 10 this month"),
            new HighscoreList("{{highscores_{0}_all}}", "Top 10 all time"),
            new HighscoreList("{{champions_{0}}}", "Champions: top 3 this month"),
        };

        /// <summary>
        /// The codes for the editors' Codes dialog: box widgets, games, and per highscore list one code per game (in the
        /// order of games). JSON with HTML-sensitive characters escaped, so it can sit inside a script element.
        /// </summary>
        public static string EditorJson() => JsonSerializer.Serialize(new
        {
            boxes = BoxCodes.Select(c => new { code = c.Value, name = c.Name, description = c.Description }),
            games = Games.Select(g => new { id = g.Id, name = g.Name }),
            highscores = HighscoreLists.Select(l => new { name = l.Name, codes = Games.Select(g => string.Format(l.Format, g.Id)) }),
        });
    }
}
