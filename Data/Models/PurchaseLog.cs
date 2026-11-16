using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>
    /// One row per purchase, written by Odin (catalogue, gift, club, tickets, song) and by the CMS
    /// (web_furni, home_store, group). Table from Odin's tools/migrations/update.1.37.sql.
    /// </summary>
    [Table("purchase_log")]
    public class PurchaseLog
    {
        public const string Catalogue = "catalogue";
        public const string Gift = "gift";
        public const string Club = "club";
        public const string Tickets = "tickets";
        public const string Song = "song";
        public const string WebFurni = "web_furni";
        public const string HomeStore = "home_store";
        public const string Group = "group";

        /// <summary>Catalogue page fuses players can buy from; any other page is a staff page.</summary>
        public static readonly string[] PlayerPageFuses = { "default", "fuse_catalogue_clubshop" };

        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("kind")]
        public string Kind { get; set; }

        [Column("receiver_id")]
        public int? ReceiverId { get; set; }

        [Column("catalogue_item_id")]
        public int? CatalogueItemId { get; set; }

        [Column("definition_id")]
        public int? DefinitionId { get; set; }

        [Column("item_name")]
        public string ItemName { get; set; }

        [Column("credits")]
        public int Credits { get; set; }

        [Column("page_fuse")]
        public string PageFuse { get; set; }

        /// <summary>"game" (Odin) or "web" (CMS).</summary>
        [Column("source")]
        public string Source { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
