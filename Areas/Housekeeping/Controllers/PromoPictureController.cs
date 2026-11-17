using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// The promo picture builder: a 425x178 promo picture (the size of the site's promos) cropped from a room photo,
    /// with a banner on top. The browser draws it (wwwroot/housekeeping/js/promo-picture.js); this stores the PNG and
    /// its settings, and points the promos that use the picture at the new version.
    /// </summary>
    [Area("Housekeeping")]
    public class PromoPictureController : Controller
    {
        public const int Width = 425, Height = 178;
        private const int MaxBytes = 2 * 1024 * 1024;

        /// <summary>Banner overlays (wwwroot/housekeeping/promo/), drawn at the picture's top-left corner.</summary>
        public static readonly IReadOnlyList<(string Id, string Name, string File)> Banners = new[]
        {
            ("rum_i_rampelyset", "Rum i rampelyset", "/housekeeping/promo/banner_rum_i_rampelyset.png"),
        };

        private readonly DataContext _context;
        private readonly IAuditLogService _audit;

        public PromoPictureController(DataContext context, IAuditLogService audit)
        {
            _context = context;
            _audit = audit;
        }

        public static string Url(PromoPicture picture) => $"/promo-picture/{picture.Id}.png?v={picture.UpdatedAt.Ticks}";

        /// <param name="id">A picture to edit.</param>
        /// <param name="roomId">The room to start a new picture from.</param>
        /// <param name="promoId">The promo the builder was opened from: saving points it at the picture.</param>
        /// <param name="pageId">The page a new promo would go on (opened from Create promo).</param>
        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> Index(int? id, int? roomId, int? promoId, int? pageId)
        {
            var picture = id == null ? null : await _context.PromoPictures.AsNoTracking().Where(p => p.Id == id)
                .Select(p => new PromoPicture { Id = p.Id, RoomId = p.RoomId, Zoom = p.Zoom, OffsetX = p.OffsetX, OffsetY = p.OffsetY, Background = p.Background, Banner = p.Banner, UpdatedAt = p.UpdatedAt })
                .FirstOrDefaultAsync();
            if (id != null && picture == null) return NotFound();

            var promo = promoId == null ? null : await _context.Promos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == promoId);
            ViewBag.Picture = picture;
            ViewBag.RoomId = picture?.RoomId ?? roomId;
            ViewBag.Promo = promo;
            ViewBag.PageId = promo?.PageId ?? pageId ?? await _context.Pages.Where(p => p.Slug == "community").Select(p => (int?)p.Id).FirstOrDefaultAsync();
            ViewBag.Pages = await _context.Pages.AsNoTracking().OrderBy(p => p.Name).Select(p => new Pages { Id = p.Id, Name = p.Name, Slug = p.Slug }).ToListAsync();
            return View();
        }

        public class SaveModel
        {
            public int? Id { get; set; }
            public int RoomId { get; set; }
            public int Zoom { get; set; }
            public int OffsetX { get; set; }
            public int OffsetY { get; set; }
            public string Background { get; set; }
            public string Banner { get; set; }
            public int? PromoId { get; set; }
            /// <summary>The picture as a base64 PNG (no data: prefix).</summary>
            public string Png { get; set; }
        }

        [HttpPost]
        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> Save([FromBody] SaveModel model)
        {
            if (model == null) return BadRequest(new { error = "Nothing to save" });
            byte[] png;
            try { png = Convert.FromBase64String(model.Png ?? ""); }
            catch (FormatException) { return BadRequest(new { error = "The picture isn't a PNG" }); }
            if (png.Length > MaxBytes) return BadRequest(new { error = "The picture is too big" });
            if (!IsPng(png, out var width, out var height) || width != Width || height != Height)
                return BadRequest(new { error = $"The picture must be a {Width}x{Height} PNG" });
            if (!await _context.Rooms.AnyAsync(r => r.Id == model.RoomId)) return BadRequest(new { error = "No such room" });
            var background = System.Text.RegularExpressions.Regex.IsMatch(model.Background ?? "", "^#[0-9a-fA-F]{6}$") ? model.Background : "#000000";
            var banner = Banners.Any(b => b.Id == model.Banner) ? model.Banner : "";

            PromoPicture picture;
            if (model.Id != null)
            {
                picture = await _context.PromoPictures.FirstOrDefaultAsync(p => p.Id == model.Id);
                if (picture == null) return NotFound(new { error = "No such picture" });
            }
            else
            {
                picture = new PromoPicture();
                _context.PromoPictures.Add(picture);
            }
            picture.RoomId = model.RoomId;
            picture.Zoom = Math.Clamp(model.Zoom, 1, 3);
            picture.OffsetX = model.OffsetX;
            picture.OffsetY = model.OffsetY;
            picture.Background = background;
            picture.Banner = banner;
            picture.Photo = png;
            picture.UpdatedAt = DateTime.Now;
            picture.UpdatedBy = int.Parse(User.Identity.Name);
            await _context.SaveChangesAsync();

            // Promos showing this picture (any version), and the promo the builder was opened from, get the new address.
            var url = Url(picture);
            var prefix = $"/promo-picture/{picture.Id}.png";
            var promos = await _context.Promos.Where(p => p.Img == prefix || p.Img.StartsWith(prefix + "?") || (model.PromoId != null && p.Id == model.PromoId)).ToListAsync();
            foreach (var promo in promos) promo.Img = url;
            await _context.SaveChangesAsync();

            await _audit.AddLog(AuditLogType.save_promo_picture, int.Parse(User.Identity.Name), picture.Id.ToString(), $"room {picture.RoomId}");
            return Json(new { id = picture.Id, url, promos = promos.Select(p => p.Id) });
        }

        /// <summary>A PNG's size from its IHDR chunk.</summary>
        private static bool IsPng(byte[] data, out int width, out int height)
        {
            width = height = 0;
            byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            if (data.Length < 24 || !data.Take(8).SequenceEqual(signature)) return false;
            width = data[16] << 24 | data[17] << 16 | data[18] << 8 | data[19];
            height = data[20] << 24 | data[21] << 16 | data[22] << 8 | data[23];
            return true;
        }
    }
}
