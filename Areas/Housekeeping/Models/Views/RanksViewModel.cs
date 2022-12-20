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
        public string Message { get; set; }

    }

    public class RankBaseViewModel {
        [Required]
        [Display(Name = "Rank Name")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
        public string Name { get; set; }
    }

    public class RanksCreateViewModel : RankBaseViewModel {
        public IEnumerable<RanksSelectedFusesModel> Fuses { get; set; }
    }

    public class RanksEditViewModel : RankBaseViewModel {
        [Required]
        public int RankId { get; set; }
        public IEnumerable<RanksSelectedFusesModel> Fuses { get; set; }
    }

    public class RanksSelectedFusesModel : KeplerCMS.Data.Models.Fuses {
        public bool Selected { get; set; }
    }

    public class RanksCreatePostModel : RankBaseViewModel {
        public string[] RankRights { get; set; }
    }

    public class RanksUpdatePostModel  : RankBaseViewModel {
        [Required]
        public int RankId { get; set; }
        public string[] RankRights { get; set; }
    }

}
