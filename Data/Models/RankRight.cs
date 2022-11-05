using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("rank_rights")]
    public class RankRight
    {
        [Column("rank_id")]
        [Key,ForeignKey("Rank")]
        public int RankId { get; set; }
        
        [Column("fuse")]
        [ForeignKey("NewFuses")]
        public string FuseName { get; set; }

        public virtual Rank Rank { get; set; }
        public virtual NewFuses Fuse { get; set; }
    }
}
