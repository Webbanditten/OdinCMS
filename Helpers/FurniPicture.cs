using System;

namespace KeplerCMS.Helpers
{
    /// <summary>Query strings for the furni imager (/habbo-imaging/furni).</summary>
    public static class FurniPicture
    {
        /// <summary>
        /// "sprite=chair_norja&amp;color=2" from "chair_norja*2". Posters need their number ("poster27"): pass
        /// ItemsDefinitions.ImageSprite (FurniService.PosterSprites) or CatalogueFurniEntry.ImageSprite, not the bare "poster".
        /// </summary>
        public static string Query(string sprite)
        {
            var parts = (sprite ?? "").Split('*', 2);
            return "sprite=" + Uri.EscapeDataString(parts[0]) + (parts.Length > 1 && parts[1].Length > 0 ? "&color=" + Uri.EscapeDataString(parts[1]) : "");
        }
    }
}
