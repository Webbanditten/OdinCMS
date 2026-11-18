using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>
    /// An ad in the hotel or on the website (Housekeeping > Ads, sql/migrations/2026-11-18_ads.sql). It rotates with the
    /// other active ads of its placement, by weight. Odin reads the same table for the in-game slots.
    /// </summary>
    [Table("cms_ads")]
    public class Ad
    {
        [Column("id")] public int Id { get; set; }
        [Column("name")] public string Name { get; set; }
        /// <summary>One of <see cref="KeplerCMS.Models.AdPlacements"/>.</summary>
        [Column("placement")] public string Placement { get; set; }
        /// <summary>A site path from the media library: /images/ads/... (an upload) or /c_images/... (a site file).</summary>
        [Column("image")] public string Image { get; set; }
        /// <summary>Billboards: "auto" (a flat picture the CMS slants to each room's frame) or the frame format the image
        /// was made for (small_left ...). Other placements: "as_is".</summary>
        [Column("fit")] public string Fit { get; set; }
        /// <summary>
        /// A flat billboard picture's crop (Housekeeping > Ads, drag the preview): the point of the picture (0..1 of its width
        /// and height) the frame's screen is centred on, and how far it is zoomed in (1 = the screen just covers the picture).
        /// </summary>
        [Column("crop_x")] public double CropX { get; set; } = 0.5;
        [Column("crop_y")] public double CropY { get; set; } = 0.5;
        [Column("crop_zoom")] public double CropZoom { get; set; } = 1;
        [Column("click_url")] public string ClickUrl { get; set; }
        [Column("starts_at")] public DateTime? StartsAt { get; set; }
        [Column("ends_at")] public DateTime? EndsAt { get; set; }
        [Column("enabled")] public bool Enabled { get; set; }
        [Column("weight")] public int Weight { get; set; }
        [Column("views")] public long Views { get; set; }
        [Column("clicks")] public long Clicks { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }
        [Column("updated_at")] public DateTime UpdatedAt { get; set; }

        public bool IsActive(DateTime now) => Enabled && (StartsAt == null || StartsAt <= now) && (EndsAt == null || EndsAt > now);
    }

    /// <summary>A billboard ad's rooms (none = every room with a billboard).</summary>
    [Table("cms_ad_rooms")]
    public class AdRoom
    {
        [Column("ad_id")] public int AdId { get; set; }
        [Column("room_id")] public int RoomId { get; set; }
    }

    /// <summary>The billboard frame of a public room: its format (small/large, left/right) and the frame's cast member.</summary>
    [Table("cms_billboard_frames")]
    public class BillboardFrame
    {
        [Column("room_id")] public int RoomId { get; set; }
        [Column("format")] public string Format { get; set; }
        [Column("frame")] public string Frame { get; set; }
        /// <summary>How far the picture's content must move right/down (-1..1) to sit on this frame's screen.</summary>
        [Column("dx")] public int Dx { get; set; }
        [Column("dy")] public int Dy { get; set; }
    }
}
