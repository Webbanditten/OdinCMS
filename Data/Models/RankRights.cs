using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("rank_rights")]
    public class RankRights
    {
        [Column("rank_id")]
        public string RankId { get; set; }
        [Key]
        [Column("fuse")]
        public string Fuse { get; set; }
    }
}
