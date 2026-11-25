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
    }
}
