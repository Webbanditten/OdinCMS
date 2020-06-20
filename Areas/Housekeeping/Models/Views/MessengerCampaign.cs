using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class MessengerCampaign
    {
        [Required]
        public string Text { get; set; }
        [Required]
        public string Url { get; set; }
        [Required]
        public string Link { get; set; }
        [Display(Name = "Only to online users")]
        public bool OnlineOnly { get; set; }
    }
}
