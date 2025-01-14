using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("rooms_bots")]
    public class Bots
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [NotNull]
        public string Name { get; set; }
        
        [Column("mission")]
        [NotNull]
        public string Mission { get; set; }

        [Column("x")]
        public int X { get; set; }
        
        [Column("y")]
        public int Y { get; set; }
        
        [Column("start_look")]
        [NotNull]
        public string StartLook { get; set; }
        
        [Column("figure")]
        [NotNull]
        public string figure { get; set; }
        
        [Column("walkspace")]
        [NotNull]
        public string Walkspace { get; set; }
        
        [Column("room_id")]
        public int RoomId { get; set; }
        
        [Column("speech")]
        [NotNull]
        public string Speech { get; set; }
        
        [Column("response")]
        [NotNull]
        public string Response { get; set; }
        
        [Column("unrecognised_response")]
        [NotNull]
        public string UnrecognisedResponse { get; set; }
        
        [Column("hand_items")]
        [NotNull]
        public string HandItems { get; set; }
        
        [NotMapped]
        public Rooms Room { get; set; }
    }
}
