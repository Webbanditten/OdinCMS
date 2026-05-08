using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    public enum FuseUserGroup {
        ANYONE = 0,
        HABBO_CLUB = 1,
        CUSTOM = 2,
    }
    [Table("fuses")]
    public class Fuses
    {
        [Key]
        [Column("fuse")]
        public string Name { get; set; }
        [Column("user_group")]
        public FuseUserGroup UserGroup { get; set; }
        [Column("description")]
        public string Description { get; set; }
    }
}
