using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class BotsListView
    {
        public IEnumerable<Bots> Bots { get; set; }
    }

    /// <summary>The bot edit page: the bot (new when Id is 0) and the public rooms it can stand in.</summary>
    public class BotEditView
    {
        public Bots Bot { get; set; }
        public List<Rooms> Rooms { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        /// <summary>handitem1..25 from the hotel's texts.</summary>
        public Dictionary<int, string> HandItemNames { get; set; } = new();
        /// <summary>The other bots, to start a look from one of them.</summary>
        public List<BotSummary> Others { get; set; } = new();
    }

    public class BotSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Room { get; set; }
        public string Figure { get; set; }
    }
}
