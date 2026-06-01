using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    [Table("cms_campaign_actions")]
    public class CampaignAction
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("campaign_id")]
        public int CampaignId { get; set; }

        [Column("action_type")]
        [Required]
        public string ActionType { get; set; }

        [Column("action_data")]
        [Required]
        public string ActionData { get; set; }

        [Column("order_index")]
        public int OrderIndex { get; set; }

        [ForeignKey("CampaignId")]
        public Campaign Campaign { get; set; }
    }
}
