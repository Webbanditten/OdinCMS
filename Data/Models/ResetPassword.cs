using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("cms_resetpassword")]
    public class ResetPassword
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("timestamp")]
        public DateTime Timestamp { get; set; }
        [Column("guid")]
        public string guid { get; set; }


    }
}
