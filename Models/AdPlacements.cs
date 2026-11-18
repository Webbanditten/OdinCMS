using System.Collections.Generic;
using System.Linq;

namespace KeplerCMS.Models
{
    /// <summary>Where an ad can be shown (cms_ads.placement), with the image size the slot uses.</summary>
    public static class AdPlacements
    {
        public const string RoomBillboard = "room_billboard";
        public const string RoomLoading = "room_loading";
        public const string Purse = "purse";
        public const string PoolScreen = "pool_screen";
        public const string WebsiteTop = "website_top";
        public const string WebsiteSide = "website_side";
        public const string WebsitePartner = "website_partner";

        public record Placement(string Id, string Name, string Size, string Where, bool InGame);

        public static readonly IReadOnlyList<Placement> All = new[]
        {
            new Placement(RoomBillboard, "Room billboard", "160×140 / 200×180 (slanted)", "The frame on the wall of 46 public rooms. One ad per room visit; pick rooms or leave it for all.", true),
            new Placement(RoomLoading, "Room loading", "up to 620×360", "The window shown while a room loads, for 4 seconds: on every ads.loading.every-th room entry (setting), at most 5 times per visit to the hotel.", true),
            new Placement(Purse, "Purse", "200×60", "The purse's transaction list (Vis transaktioner) in the browser client. One ad per visit to the hotel.", true),
            new Placement(PoolScreen, "Pool screen", "108×102", "The big screen in Habbo Poolen II and on Tagterassen, for 12 seconds every ads.pool.every-th camera turn (setting). It can't be clicked.", true),
            new Placement(WebsiteTop, "Website top banner", "728×90", "The top of every page on the website. One ad per page view.", false),
            new Placement(WebsiteSide, "Website right column", "160×600", "The right column of most pages. One ad per page view.", false),
            new Placement(WebsitePartner, "Website partner box", "168×120", "The small box above the right column. One ad per page view.", false),
        };

        public static Placement Get(string id) => All.FirstOrDefault(p => p.Id == id);

        /// <summary>The image size an ad of this placement is shown at (billboards: the small left frame).</summary>
        public static (int Width, int Height) MaxSize(string id) => id switch
        {
            RoomBillboard => (200, 180),
            RoomLoading => (620, 360),
            Purse => (200, 60),
            PoolScreen => (108, 102),
            WebsiteTop => (728, 90),
            WebsiteSide => (160, 600),
            WebsitePartner => (168, 120),
            _ => (728, 600),
        };
    }

    /// <summary>
    /// The billboard frames: every public room's screen is 160×140 (small) or 200×180 (large), a slanted parallelogram facing
    /// left (rising to the right) or right, with its vertical edges 60 or 80 px high. Measured from the room casts.
    /// </summary>
    public static class BillboardFormats
    {
        public const string Auto = "auto";

        public record Format(string Id, string Name, int Width, int Height, bool Left)
        {
            public int FaceHeight => Height - Width / 2;
        }

        public static readonly IReadOnlyList<Format> All = new[]
        {
            new Format("small_left", "Small, facing left", 160, 140, true),
            new Format("small_right", "Small, facing right", 160, 140, false),
            new Format("large_left", "Large, facing left", 200, 180, true),
            new Format("large_right", "Large, facing right", 200, 180, false),
        };

        public static Format Get(string id) => All.FirstOrDefault(f => f.Id == id) ?? All[0];
    }
}
