﻿using System;
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
        [Column("birthday")]
        public string Birthday { get; set; }
        [Column("club_subscribed")]
        public double ClubSubscribed { get; set; }
        [Column("club_expiration")]
        public double ClubExpiration { get; set; }
        [Column("club_gift_due")]
        public double ClubGiftDue { get; set; }
        [Column("created_at")]
        public DateTime CreateAt { get; set; }
        [Column("last_online")]
        public long LastOnlineTimestamp { get; set; }
        [Column("sex")]
        public string Gender { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("rank")]
        public int Rank { get; set; }
        [Column("badge")]
        public string Badge { get; set; }
        [Column("Motto")] 
        public string Motto { get; set; }
        [Column("badge_active")]
        public int IBadgeActive { get; set; }

        [Column("sso_ticket")]
        public string SSOTicket { get; set; }
        [Column("status")]
        public string Status { get; set; }
        [Column("credits")]
        public int Credits { get; set; }
        [Column("battleball_points")]
        public int BattleballPoints { get; set; }
        
        [Column("snowstorm_points")]
        public int SnowStormPoints { get; set; }

        [Column("group_id")]
        public int Group { get; set; }
        [Column("online_time_seconds")]
        public int OnlineTimeInSeconds { get; set; }
        [Column("times_logged_in")]
        public int TimesLoggedIn { get; set; }

        public bool HasHabboClub
        {
            get {
                var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
                return this.ClubExpiration > timeSpan.TotalSeconds;
            }
        }

        [NotMapped]
        public bool BadgeActive
        {
            get { return IBadgeActive == 1; }
            set { IBadgeActive = value ? 1 : 0; }
        }
        
        [NotMapped]
        public DateTime LastOnline
        {
            get
            {
                var unixDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                unixDate = unixDate.AddSeconds(this.LastOnlineTimestamp).ToLocalTime();
                return unixDate;
            }
        }

        [NotMapped]
        public int HabboClubInDays
        {
            get
            {
                var today = DateTime.UtcNow;
                var expirationDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                expirationDate = expirationDate.AddSeconds(ClubExpiration).ToLocalTime();
                
                var days = (expirationDate - today).Days;
                return days > 0 ? days : 0;
            }
        }

        [NotMapped]
        public IEnumerable<RankRights> Fuses { get; set; }
    }
}
