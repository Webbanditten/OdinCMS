using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class RanksViewModel
    {
        public IEnumerable<KeplerCMS.Data.Models.Rank> Ranks { get; set; }

    }

    public class RanksCreateViewModel {
        [Required]
        [Display(Name = "Rank Name")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
        public string Name { get; set; }
        public IEnumerable<RanksSelectedFusesModel> Fuses { get; set; }
    }

    public class RanksSelectedFusesModel : KeplerCMS.Data.Models.NewFuses {
        public bool Selected { get; set; }
    }

    public class RanksCreatePostModel {
        [Required]
        [Display(Name = "Rank Name")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
        public string Name { get; set; }
        public string[] RankRights { get; set; }
    }

}
