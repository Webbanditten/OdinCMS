using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("settings")]
    public class Settings
    {
        [Key]
        [Display(Name = "Setting")]
        [Column("setting")]
        public string Setting { get; set; }
        [Display(Name = "Value")]
        [Column("value")]
        public string Value { get; set; }


    }
}
