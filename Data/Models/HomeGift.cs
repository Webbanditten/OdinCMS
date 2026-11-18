using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>
    /// A free Habbo Home gift a player has fetched ({xmas_home_gift}: "xmas-2026" = that Christmas's decorations). One row
    /// per gift and player (unique), so a gift can't be fetched twice.
    /// </summary>
    [Table("cms_home_gifts")]
    public class HomeGift
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("gift_key")]
        public string GiftKey { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("claimed_at")]
        public DateTime ClaimedAt { get; set; }
    }
}
