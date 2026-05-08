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
        [Display(Name = "Ban time")]
        public int BanLength { get; set; }
        [Display(Name = "Extra info")]
        [Required]
        [MinLength(1, ErrorMessage = "Please enter extra info")]
        public string ExtraInfo { get; set; }
        [Display(Name="Also ban computer for more than one hour")]
        public bool BanMachine { get; set; }
        [Display(Name="Also ban IP for more than one hour")]
        public bool BanIP { get; set; }
        public string Action { get; set; }
    }
    
    public class BansViewModel
    {
        public IEnumerable<UsersBans> Bans { get; set; }
        public int CurrentPage { get; set; }
        public string Filter { get; set; }
        public int TotalPages { get; set; }
        public string Search { get; set; }
        public string Letter { get; set; }

    }

    public class UpdateInfobusModel
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }
    

}
