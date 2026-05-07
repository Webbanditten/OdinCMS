using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    [Table("calls_for_help")]
    public class CallsForHelp
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("caller_id")]
        public int CallerId { get; set; }
        [Column("caller_username")]
        public string CallerUsername { get; set; }
        [Column("message")]
        public string Message { get; set; }
        [Column("room_id")]
        public int RoomId { get; set; }
        [Column("room_name")]
        public string RoomName { get; set; }
        [Column("category")]
        public int Category { get; set; }
        [Column("picked_up_by")]
        public int PickedUpBy { get; set; }
        [Column("picked_up_username")]
        public string PickedUpUsername { get; set; }
        [Column("picked_up_at")]
        public DateTime? PickedUpAt { get; set; }
        [Column("closed_at")]
        public DateTime? ClosedAt { get; set; }
        [Column("closed_reason")]
        public string ClosedReason { get; set; }
        [Column("reply_message")]
        public string ReplyMessage { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
