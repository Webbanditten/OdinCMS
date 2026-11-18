using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Implementations;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// Housekeeping > Ads: every ad slot in the hotel and on the website (AdPlacements). The page (Views/Ads/Index) lists the
    /// ads per placement with views and clicks and edits them through the JSON actions below (wwwroot/housekeeping/js/ads.js).
    /// A save tells Odin to reload its ads (reload_ads), so in-game slots change on the next room entry.
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.housekeeping_pages)]
    public class AdsController : Controller
    {
        private readonly DataContext _context;
        private readonly IAdService _ads;
        private readonly ICommandQueueService _commandQueue;
        private readonly IAuditLogService _audit;
        private readonly AdCounters _counters;

        public AdsController(DataContext context, IAdService ads, ICommandQueueService commandQueue, IAuditLogService audit, AdCounters counters)
        {
            _context = context;
            _ads = ads;
            _commandQueue = commandQueue;
            _audit = audit;
            _counters = counters;
        }

        public class AdRow
        {
            public Ad Ad { get; set; }
            public List<int> Rooms { get; set; }
        }

        public class FrameRoom
        {
            public int RoomId { get; set; }
            public string Name { get; set; }
            public string Format { get; set; }
        }

        public async Task<IActionResult> Index()
        {
            await _counters.Flush();
            var ads = await _context.Ads.AsNoTracking().OrderBy(a => a.Placement).ThenByDescending(a => a.Enabled).ThenBy(a => a.Name).ToListAsync();
            var rooms = await _context.AdRooms.AsNoTracking().ToListAsync();
            ViewData["frames"] = await BillboardRooms();
            ViewData["poolRooms"] = await PoolScreenRooms();
            ViewData["publicRooms"] = await _context.Rooms.AsNoTracking().Where(r => r.OwnerId == 0).OrderBy(r => r.Name).ThenBy(r => r.Id)
                .Select(r => new FrameRoom { RoomId = r.Id, Name = r.Name }).ToListAsync();
            var ids = rooms.Select(r => r.RoomId).Distinct().ToList();
            ViewData["roomNames"] = await _context.Rooms.AsNoTracking().Where(r => ids.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);
            return View(ads.Select(a => new AdRow { Ad = a, Rooms = rooms.Where(r => r.AdId == a.Id).Select(r => r.RoomId).ToList() }).ToList());
        }

        /// <summary>The rooms with the big pool screen (Habbo Poolen II, Tagterassen).</summary>
        private async Task<List<FrameRoom>> PoolScreenRooms() =>
            await _context.Rooms.AsNoTracking().Where(r => AdPlacements.PoolScreenModels.Contains(r.Model))
                .OrderBy(r => r.Id).Select(r => new FrameRoom { RoomId = r.Id, Name = r.Name, Format = r.Model }).ToListAsync();

        /// <summary>
        /// The room loading ad's chosen rooms (any room, public or private) with their names and owners, for the picker
        /// when an ad is opened. The picker searches with the editors' room search (EditorController.Rooms).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Rooms(string ids)
        {
            var list = ParseIds(ids);
            var rooms = await _context.Rooms.AsNoTracking().Where(r => list.Contains(r.Id)).Select(r => new { r.Id, r.Name, r.OwnerId }).ToListAsync();
            var ownerIds = rooms.Select(r => r.OwnerId).Where(o => o > 0).Distinct().ToList();
            var owners = await _context.Users.AsNoTracking().Where(u => ownerIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username);
            return Json(list.Select(id =>
            {
                var r = rooms.FirstOrDefault(x => x.Id == id);
                return r == null ? new { id, name = (string)null, owner = (string)null }
                    : new { id, name = r.Name, owner = r.OwnerId > 0 ? owners.GetValueOrDefault(r.OwnerId) ?? "#" + r.OwnerId : null };
            }));
        }

        private static List<int> ParseIds(string ids) =>
            (ids ?? "").Split(new[] { ',', ' ', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0).Where(id => id > 0).Distinct().Take(200).ToList();

        private async Task<List<FrameRoom>> BillboardRooms()
        {
            var frames = await _context.BillboardFrames.AsNoTracking().ToListAsync();
            var ids = frames.Select(f => f.RoomId).ToList();
            var names = await _context.Rooms.AsNoTracking().Where(r => ids.Contains(r.Id)).Select(r => new { r.Id, r.Name }).ToListAsync();
            return frames.Select(f => new FrameRoom { RoomId = f.RoomId, Format = f.Format, Name = names.FirstOrDefault(n => n.Id == f.RoomId)?.Name ?? "#" + f.RoomId })
                .OrderBy(f => f.Name).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> Ad(int id)
        {
            var ad = await _context.Ads.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (ad == null) return NotFound();
            var rooms = await _context.AdRooms.AsNoTracking().Where(r => r.AdId == id).Select(r => r.RoomId).ToListAsync();
            return Json(new { ad, rooms });
        }

        public class SaveRequest
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Placement { get; set; }
            public string Image { get; set; }
            public string Fit { get; set; }
            public double CropX { get; set; } = 0.5;
            public double CropY { get; set; } = 0.5;
            public double CropZoom { get; set; } = 1;
            public string ClickUrl { get; set; }
            public DateTime? StartsAt { get; set; }
            public DateTime? EndsAt { get; set; }
            public bool Enabled { get; set; }
            public int Weight { get; set; }
            public List<int> Rooms { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveRequest request)
        {
            if (request == null) return BadRequest("Missing ad.");
            var name = (request.Name ?? "").Trim();
            if (name.Length == 0 || name.Length > 100) return BadRequest("Give the ad a name (at most 100 characters).");
            var placement = AdPlacements.Get(request.Placement);
            if (placement == null) return BadRequest("Pick where the ad is shown.");
            var image = (request.Image ?? "").Trim();
            if (image.Length == 0 || image.Length > 255) return BadRequest("Pick an image.");
            if (await _ads.LoadImage(image) == null) return BadRequest("The image wasn't found: " + image);
            var clickUrl = string.IsNullOrWhiteSpace(request.ClickUrl) ? null : request.ClickUrl.Trim();
            if (clickUrl != null && (clickUrl.Length > 500 || KeplerCMS.Controllers.AdsController.SafeTarget(clickUrl) == null))
                return BadRequest("The link must be a page on the site (/credits) or an http(s) address.");
            if (request.StartsAt != null && request.EndsAt != null && request.EndsAt <= request.StartsAt) return BadRequest("The ad ends before it starts.");

            string fit = "as_is";
            var rooms = new List<int>();
            var requested = (request.Rooms ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
            if (placement.Id == AdPlacements.RoomLoading && requested.Count > 0)
            {
                var existing = await _context.Rooms.AsNoTracking().Where(r => requested.Contains(r.Id)).Select(r => r.Id).ToListAsync();
                var missing = requested.Except(existing).ToList();
                if (missing.Count > 0) return BadRequest("No room has the ID " + string.Join(", ", missing) + ".");
                rooms = requested;
            }
            if (placement.Id == AdPlacements.PoolScreen)
            {
                var pool = (await PoolScreenRooms()).Select(r => r.RoomId).ToList();
                rooms = requested.Where(pool.Contains).ToList();
            }
            if (placement.Id == AdPlacements.RoomBillboard)
            {
                fit = request.Fit == BillboardFormats.Auto || BillboardFormats.All.Any(f => f.Id == request.Fit) ? request.Fit : BillboardFormats.Auto;
                var frames = await _context.BillboardFrames.AsNoTracking().ToListAsync();
                rooms = (request.Rooms ?? new List<int>()).Distinct()
                    .Where(id => frames.Any(f => f.RoomId == id && (fit == BillboardFormats.Auto || f.Format == fit))).ToList();
                if (fit != BillboardFormats.Auto && rooms.Count == 0 && (request.Rooms?.Count ?? 0) > 0)
                    return BadRequest("None of the chosen rooms has a frame of this format.");
            }

            var ad = request.Id > 0 ? await _context.Ads.FirstOrDefaultAsync(a => a.Id == request.Id) : null;
            if (request.Id > 0 && ad == null) return NotFound();
            var creating = ad == null;
            if (creating)
            {
                ad = new Ad { CreatedAt = DateTime.Now };
                _context.Ads.Add(ad);
            }
            ad.Name = name;
            ad.Placement = placement.Id;
            ad.Image = image;
            ad.Fit = fit;
            ad.CropX = Math.Clamp(double.IsFinite(request.CropX) ? request.CropX : 0.5, 0, 1);
            ad.CropY = Math.Clamp(double.IsFinite(request.CropY) ? request.CropY : 0.5, 0, 1);
            ad.CropZoom = Math.Clamp(double.IsFinite(request.CropZoom) ? request.CropZoom : 1, 1, 4);
            ad.ClickUrl = clickUrl;
            ad.StartsAt = request.StartsAt;
            ad.EndsAt = request.EndsAt;
            ad.Enabled = request.Enabled;
            ad.Weight = Math.Clamp(request.Weight, 1, 100);
            ad.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _context.AdRooms.RemoveRange(await _context.AdRooms.Where(r => r.AdId == ad.Id).ToListAsync());
            _context.AdRooms.AddRange(rooms.Select(r => new AdRoom { AdId = ad.Id, RoomId = r }));
            await _context.SaveChangesAsync();

            await _audit.AddLog(creating ? AuditLogType.create_ad : AuditLogType.edit_ad, int.Parse(User.Identity.Name), ad.Name, placement.Name, 0, ad.Id);
            if (placement.InGame) _commandQueue.QueueCommand(CommandQueueType.reload_ads, new CommandTemplate());
            return Json(new { id = ad.Id });
        }

        public class IdRequest
        {
            public int Id { get; set; }
            public bool Enabled { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Toggle([FromBody] IdRequest request)
        {
            var ad = await _context.Ads.FirstOrDefaultAsync(a => a.Id == request.Id);
            if (ad == null) return NotFound();
            ad.Enabled = request.Enabled;
            ad.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await _audit.AddLog(AuditLogType.edit_ad, int.Parse(User.Identity.Name), ad.Name, request.Enabled ? "on" : "off", 0, ad.Id);
            if (AdPlacements.Get(ad.Placement)?.InGame == true) _commandQueue.QueueCommand(CommandQueueType.reload_ads, new CommandTemplate());
            return Json(new { ok = true });
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] IdRequest request)
        {
            var ad = await _context.Ads.FirstOrDefaultAsync(a => a.Id == request.Id);
            if (ad == null) return NotFound();
            _context.AdRooms.RemoveRange(await _context.AdRooms.Where(r => r.AdId == ad.Id).ToListAsync());
            _context.Ads.Remove(ad);
            await _context.SaveChangesAsync();
            await _audit.AddLog(AuditLogType.delete_ad, int.Parse(User.Identity.Name), ad.Name, AdPlacements.Get(ad.Placement)?.Name, 0, ad.Id);
            if (AdPlacements.Get(ad.Placement)?.InGame == true) _commandQueue.QueueCommand(CommandQueueType.reload_ads, new CommandTemplate());
            return Json(new { ok = true });
        }

        /// <summary>The form's billboard preview: the picture slanted to a frame format, before it is saved.</summary>
        [HttpGet]
        public async Task<IActionResult> Preview(string image, string format, double x = 0.5, double y = 0.5, double zoom = 1)
        {
            var source = await _ads.LoadImage(image);
            if (source == null) return NotFound();
            var png = AdService.Slant(source.Value.Bytes, BillboardFormats.Get(format), 0, 0, x, y, zoom);
            if (png == null) return BadRequest("Couldn't read the image.");
            return File(png, "image/png");
        }
    }
}
