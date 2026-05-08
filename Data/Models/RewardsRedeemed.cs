using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("rewards_redeemed")]
    public class RewardsRedeemed
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("reward_id")]
        public int RewardId { get; set; }

        [Column("rewarded_date")]
        public DateTime RewardedDate { get; set; }
        
        [Column("user_id")]
        public int UserId { get; set; }
    }
}
