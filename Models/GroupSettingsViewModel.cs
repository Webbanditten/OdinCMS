using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Models
{
    public class GroupSettingsViewModel
    {
        public List<Rooms> Rooms { get; set; }
        public Homes Details { get; set; }
    }

    public class GroupBadgeViewModel {
        public Homes Homes { get; set; }
        public IEnumerable<Settings> Settings { get; set; }
    }

}
