using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    [Table("cms_campaign_snapshots")]
    public class CampaignSnapshot
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("campaign_id")]
        public int CampaignId { get; set; }

        [Column("action_id")]
        public int ActionId { get; set; }

        [Column("conflict_key")]
        [Required]
        public string ConflictKey { get; set; }

        [Column("original_data")]
        [Required]
        public string OriginalData { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [ForeignKey("CampaignId")]
        public Campaign Campaign { get; set; }

        [ForeignKey("ActionId")]
        public CampaignAction Action { get; set; }
    }
}
