using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>A named list of furni that reward calendars pick from (sql/migrations/2026-11-17_reward_calendars.sql).</summary>
    [Table("cms_reward_pools")]
    public class RewardPool
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; } = "";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<RewardPoolItem> Items { get; set; } = new();
    }

    /// <summary>A furni in a pool. Calendars that pick "in order" follow SortOrder.</summary>
    [Table("cms_reward_pool_items")]
    public class RewardPoolItem
    {
        [Column("pool_id")]
        public int PoolId { get; set; }

        [Column("definition_id")]
        public int DefinitionId { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        /// <summary>Suggested by the theme pass with medium confidence: staff should check it.</summary>
        [Column("needs_review")]
        public bool NeedsReview { get; set; }

        /// <summary>Why the theme pass suggested it.</summary>
        [Column("note")]
        public string Note { get; set; } = "";
    }

    /// <summary>A saved calendar plan (Models/RewardPlan.cs as JSON), reused with a new start date.</summary>
    [Table("cms_reward_templates")]
    public class RewardTemplate
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("plan")]
        public string Plan { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
