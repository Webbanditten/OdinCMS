using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    [Table("users_ip_logs")]
    public class UsersIpLogs
    {
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("ip_address")]
        public string IpAddress { get; set; }
    }
}
