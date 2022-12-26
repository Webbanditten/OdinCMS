using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{

    [Table("rank_badges")]
    public class RankBadges
    {
        [Key]
        [Column("rank")]
        public int Rank { get; set; }
        [Column("badge")]
        [StringLength(3)]
        public string Badge { get; set; }
    }
}
