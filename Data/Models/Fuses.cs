using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("rank_fuserights")]
    public class Fuses
    {
        [Key]
        [Column("min_rank")]
        public int MinRank { get; set; }
        [Column("fuseright")]
        public string Fuse { get; set; }
    }
}
