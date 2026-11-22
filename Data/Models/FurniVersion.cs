using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>
    /// One publish of a furni from the Housekeeping furni editor (docs/furni-editor-housekeeping.md): revision n of a
    /// sprite, whose casts are kept as furni-history/&lt;sprite&gt;/r&lt;n&gt;.cct (+ r&lt;n&gt;_s.cct). A sprite with rows here
    /// was made in the editor, and only those may be published again; Odin sends the highest revision per sprite in the
    /// furni revision list, so clients download a new revision instead of a cached one. Table from Odin's update.1.51.sql.
    /// </summary>
    [Table("furni_versions")]
    public class FurniVersion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("sprite")]
        public string Sprite { get; set; }
        [Column("revision")]
        public int Revision { get; set; }
        [Column("large_sha256")]
        public string LargeSha256 { get; set; }
        [Column("large_bytes")]
        public int LargeBytes { get; set; }
        [Column("small_sha256")]
        public string SmallSha256 { get; set; }
        [Column("small_bytes")]
        public int? SmallBytes { get; set; }
        [Column("definition_id")]
        public int? DefinitionId { get; set; }
        /// <summary>The definition as it was saved with this revision (JSON of CatalogueFurni), for a roll back.</summary>
        [Column("definition")]
        public string Definition { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        /// <summary>editor | rollback</summary>
        [Column("source")]
        public string Source { get; set; }
        [Column("rolled_back_from")]
        public int? RolledBackFrom { get; set; }
        [Column("note")]
        public string Note { get; set; }
    }
}
