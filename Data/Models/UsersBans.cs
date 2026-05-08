using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    public enum BanTypes {
        MACHINE_ID = 0,
        IP_ADDRESS = 1,
        USER_ID = 2
    }
    [Table("users_bans")]
    public class UsersBans
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("ban_type")]
        public string BanType { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("ip")]
        public string Ip { get; set; }
        [Column("message")]
        public string Message { get; set; }
        [Column("banned_until")]
        public int BannedUntilTimestamp { get; set; }
        
        [NotMapped]
        public DateTime BannedUntil
        {
            get
            {
                var unixDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                unixDate = unixDate.AddSeconds(this.BannedUntilTimestamp).ToLocalTime();
                return unixDate;
            }
        }
        [NotMapped]
        public string Username { get; set; }
    }
}
