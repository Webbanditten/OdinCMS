using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("messenger_requests")]
    public class FriendRequests
    {
        [Key]
        [Column("from_id")]
        public int FromId { get; set; }
        [Key]
        [Column("to_id")]
        public int ToId { get; set; }
    }
}
