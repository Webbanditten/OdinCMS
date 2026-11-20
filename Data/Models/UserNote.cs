using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>
    /// A staff note on a Habbo (Housekeeping > the user page, "Staff notes"), like the notes on Sulake's Habbo information
    /// tool: what every moderator should know before acting on them. Only staff see them.
    /// </summary>
    [Table("cms_user_notes")]
    public class UserNote
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("staff_id")]
        public int StaffId { get; set; }
        [Column("note")]
        public string Note { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
