using System.Collections.Generic;

namespace KeplerCMS.Models
{
    /// <summary>
    /// The codes housekeeping can write into page boxes and news text. The editors list them (the "Codes" menu and the
    /// overview under each editor, Areas/Housekeeping/Views/Shared/_ContentCodes.cshtml).
    /// Box codes are rendered by Views/Shared/Containers/Container.cshtml, highscore codes by <see cref="HighscoreWidget"/>.
    /// </summary>
    public static class ContentCodes
    {
        public const string Credits = "{credits}";
        public const string Rewards = "{rewards}";
        public const string LastOnline = "{lastonline}";
        public const string Staff = "{staff}";
        public const string PopularFurni = "{popular_furni}";
        public const string Xmas2006 = "{xmas2006}";

        public record Code(string Value, string Name, string Description);

        /// <summary>
        /// Page boxes only. A box with one of these shows just that widget (its title and theme stay); any other text
        /// in the box is not shown. Checked in this order when a box holds several.
        /// </summary>
        public static readonly IReadOnlyList<Code> BoxCodes = new[]
        {
            new Code(Credits, "Credits", "The visitor's credit balance and the voucher code form (logged out: a log-in hint)."),
            new Code(Xmas2006, "Christmas calendar", "December's rewards as the 2006 advent calendar."),
            new Code(Rewards, "Daily rewards", "Rewards from 15 days back to 15 days ahead, today's highlighted."),
            new Code(LastOnline, "Last online", "The 30 Habbos who logged in most recently."),
            new Code(Staff, "Staff", "Every rank with members, with their avatars."),
            new Code(PopularFurni, "Popular furni", "The furni players bought most in the last 30 days."),
        };

        public record Game(string Id, string Name);

        public static readonly IReadOnlyList<Game> Games = new[]
        {
            new Game("battleball", "Battle Ball"),
            new Game("snowstorm", "SnowStorm"),
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
    }

    /// <summary>The overview under an editor: the textarea's id, and whether box codes apply (page boxes, not news).</summary>
    public record ContentCodesPanel(string EditorId, bool BoxCodes);
}
