using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    [Table("users_machineid_logs")]
    public class UsersMachineIdLogs
    {
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("machine_id")]
        public string MachineId { get; set; }
    }
}
