using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("tags")]
    public class Tags
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("tag")]
        public string Tag { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("group_id")]
        public int GroupId { get; set; }
        
        [NotMapped]
        public bool CanEdit { get; set; }
    }
}
