using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("housekeeping_audit_log")]
    public class AuditLog
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }
        [Column("action")]
        public string Action { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("target_id")]
        public int TargetId { get; set; }
        [Column("message")]
        public string Message { get; set; }
        [Column("extra_notes")]
        public string ExtraNotes { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("data_id")]
        public int? DataId { get; set; }
        
    }
}
