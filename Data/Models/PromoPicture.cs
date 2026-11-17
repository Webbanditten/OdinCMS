using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>
    /// A promo picture built from a room photo in housekeeping (Promo picture builder): a 425x178 crop with a banner
    /// such as "Rum i rampelyset". The settings are kept so it can be edited again. Served at /promo-picture/ID.png.
    /// </summary>
    [Table("cms_promo_pictures")]
    public class PromoPicture
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("room_id")]
        public int RoomId { get; set; }
        [Column("zoom")]
        public int Zoom { get; set; }
        /// <summary>Where the photo's top-left corner sits in the picture (picture pixels).</summary>
        [Column("offset_x")]
        public int OffsetX { get; set; }
        [Column("offset_y")]
        public int OffsetY { get; set; }
        /// <summary>Colour behind the photo's transparent parts, #rrggbb.</summary>
        [Column("background")]
        public string Background { get; set; }
        /// <summary>Banner overlay id (PromoPictureBanners), empty for none.</summary>
        [Column("banner")]
        public string Banner { get; set; }
        [Column("photo")]
        public byte[] Photo { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [Column("updated_by")]
        public int? UpdatedBy { get; set; }
    }
}
