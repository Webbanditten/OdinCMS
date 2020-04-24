using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("users")]
    public class Users
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("username")]
        public string Username { get; set; }
        [Column("password")]
        public string Password { get; set; }
        [Column("figure")]
        public string Figure { get; set; }
        [Column("club_subscribed")]
        public double ClubSubscribed { get; set; }
        [Column("club_expiration")]
        public double ClubExpiration { get; set; }
        [Column("club_gift_due")]
        public double ClubGiftDue { get; set; }
        [Column("sex")]
        public string Gender { get; set; }

        public bool hasHabboClub()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return this.ClubExpiration > timeSpan.TotalSeconds;
        }
    }
}
