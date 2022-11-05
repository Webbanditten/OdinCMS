using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class RanksViewModel
    {
        public IEnumerable<KeplerCMS.Data.Models.NewFuses> Fuses { get; set; }
        public IEnumerable<KeplerCMS.Data.Models.Rank> Ranks { get; set; }
        public IEnumerable<KeplerCMS.Data.Models.RankRight> RankRights { get; set; }

    }

}
