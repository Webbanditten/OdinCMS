using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("rooms")]
    public class Rooms
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("owner_id")]
        [ForeignKey("Users")]
        public int OwnerId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("description")]
        public string Description { get; set; }
        [Column("accesstype")]
        public int AccessType { get; set; }
        [Column("showname")]
        public int ShowOwner { get; set; }
        [Column("category")]
        public int Category { get; set; }
        [Column("model")]
        public string Model { get; set; }
        [Column("ccts")]
        public string Casts { get; set; }
        [Column("visitors_now")]
        public int VisitorsNow { get; set; }
        [Column("visitors_max")]
        public int VisitorsMax { get; set; }
        /// <summary>How many may wait in the queue when the room is full (0 = no queue). Odin's RoomQueueManager.</summary>
        [Column("queue_max")]
        public int QueueMax { get; set; }
        /// <summary>Spectator seats: watch the room on the client's TV screen, without an avatar (0 = none).</summary>
        [Column("spectators_max")]
        public int SpectatorsMax { get; set; }
        /// <summary>Not in the navigator (it can still be entered). Odin's RoomData.isNavigatorHide.</summary>
        [Column("is_hidden")]
        public bool IsHidden { get; set; }
        /// <summary>A public room's place in its navigator category (Housekeeping > Public rooms; Odin v1.31.0).</summary>
        [Column("navigator_order")]
        public int NavigatorOrder { get; set; }
        
        public Users Owner { get; set; }
    }
}
