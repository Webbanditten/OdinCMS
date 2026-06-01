using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KeplerCMS.Models.Enums;

namespace KeplerCMS.Data.Models
{
    [Table("cms_campaigns")]
    public class Campaign
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [Required]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("activated_at")]
        public DateTime? ActivatedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("created_by_id")]
        public int CreatedById { get; set; }

        [Column("cloned_from_id")]
        public int? ClonedFromId { get; set; }

        [NotMapped]
        public CampaignStatus StatusEnum
        {
            get => Enum.TryParse<CampaignStatus>(Status, true, out var result) ? result : CampaignStatus.Draft;
            set => Status = value.ToString().ToLower();
        }

        [ForeignKey("CreatedById")]
        public Users CreatedBy { get; set; }

        [ForeignKey("ClonedFromId")]
        public Campaign ClonedFrom { get; set; }

        public List<CampaignAction> Actions { get; set; } = new List<CampaignAction>();
        public List<CampaignSnapshot> Snapshots { get; set; } = new List<CampaignSnapshot>();
    }
}
