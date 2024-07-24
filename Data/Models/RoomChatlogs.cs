using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using MySql.EntityFrameworkCore.DataAnnotations;

namespace KeplerCMS.Data.Models
{
    [Table("room_chatlogs")]
    public class RoomChatlogs
    {
        [Column("timestamp")]
        public int Timestamp { get; set; }
        [Column("room_id")]
        [ForeignKey("Rooms")]
        public int RoomId { get; set; }
        [Column("user_id")]
        [ForeignKey("Users")]
        public int UserId { get; set; }
        [Column("message")]
        public string Message { get; set; }
        [Column("chat_type")]
        public int ChatType { get; set; }
        
        public string Username { get; set; }
        public string RoomName { get; set; }
        public Users User { get; set; }
        public Rooms Room { get; set; }
    }
}
