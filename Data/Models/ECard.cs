using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>
    /// A 2006 Christmas e-card (EcardController, wwwroot/flash/xmas06/ecard.swf). Code is the share link's id
    /// (/xmas/julekort?card=CODE); CardXml is what the card saved (background, Habbo, headline, message) and loads again.
    /// No e-mail addresses are kept: a card e-mailed to the friend only records when.
    /// </summary>
    [Table("cms_ecards")]
    public class ECard
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("code")]
        public string Code { get; set; }
        [Column("user_id")]
        public int? UserId { get; set; }
        [Column("habbo_name")]
        public string HabboName { get; set; }
        /// <summary>The sender's name as written on the card ("Dit navn").</summary>
        [Column("from_name")]
        public string FromName { get; set; }
        [Column("to_name")]
        public string ToName { get; set; }
        /// <summary>The friend's Habbo name, if the sender wrote one ("Din vens Habbo-navn").</summary>
        [Column("to_habbo")]
        public string ToHabbo { get; set; }
        [Column("headline")]
        public string Headline { get; set; }
        [Column("message")]
        public string Message { get; set; }
        [Column("card_xml")]
        public string CardXml { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        /// <summary>When the card was e-mailed to the friend (logged-in senders; the address itself isn't kept).</summary>
        [Column("emailed_at")]
        public DateTime? EmailedAt { get; set; }
    }
}
