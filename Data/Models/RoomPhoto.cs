using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>The latest photo of a room, taken by the room photographer (housekeeping → room → Take photo).</summary>
    [Table("cms_room_photos")]
    public class RoomPhoto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("room_id")]
        public int RoomId { get; set; }
        /// <summary>PNG with a transparent background, cropped to the room.</summary>
        [Column("photo")]
        public byte[] Photo { get; set; }
        [Column("width")]
        public int Width { get; set; }
        [Column("height")]
        public int Height { get; set; }
        [Column("taken_at")]
        public DateTime TakenAt { get; set; }
        [Column("taken_by")]
        public int? TakenBy { get; set; }
    }
}
