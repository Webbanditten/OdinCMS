using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class AlertViewModel
    {
        [Display(Name = "The Recipient")]
        [MinLength(1, ErrorMessage = "Please enter a username")]
        [Required]
        public string Username { get; set; }
        [Display(Name = "Message")]
        [MinLength(1, ErrorMessage = "Please enter a message")]
        [Required]
        public string Message { get; set; }
        
        public string SuccessMessage { get; set; }
    }
    
    
    public class BanViewModel
    {
        [Display(Name = "The bad guy")]
        [MinLength(1, ErrorMessage = "Please enter a username")]
        [Required]
        public string Username { get; set; }
        [Display(Name = "Message")]
        [MinLength(1, ErrorMessage = "Please enter a message")]
        [Required]
        public string Message { get; set; }
        
        public string SuccessMessage { get; set; }
        public int BanLength { get; set; }
        public string ExtraInfo { get; set; }
    }
    
    

}
