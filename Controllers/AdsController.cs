using System;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Controllers
{
    /// <summary>
    /// Ads seen by players: /ads/img/{id} is the picture the game client downloads (counted as a view; billboards are
    /// slanted to the room's frame with ?f=small_left etc., moved by the room's &dx=&dy=), /ads/click/{id} counts the click and redirects to the ad's
    /// link. Odin builds both URLs from its ads.url setting. Website slots render through Views/Shared/Ads/_AdSlot.
    /// </summary>
    [Route("ads")]
    public class AdsController : Controller
    {
        private readonly DataContext _context;
        private readonly IAdService _ads;

        public AdsController(DataContext context, IAdService ads)
        {
            _context = context;
            _ads = ads;
        }

        [HttpGet("img/{id:int}")]
        public async Task<IActionResult> Image(int id, string f = null, int dx = 0, int dy = 0)
        {
            var ad = await _context.Ads.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (ad == null) return NotFound();

            (byte[] Bytes, string ContentType)? image;
            if (ad.Placement == AdPlacements.RoomBillboard)
            {
                image = await _ads.RenderBillboard(ad, BillboardFormats.Get(f).Id, dx, dy);
            }
            else
            {
                image = await _ads.LoadImage(ad.Image);
            }
            if (image == null) return NotFound();

            if (ad.IsActive(DateTime.Now)) _ads.CountView(ad.Id);
            // The game client fetches it from the hotel page (same origin) or, locally, from the DirPlayer harness.
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Cache-Control"] = "no-store";
            return File(image.Value.Bytes, image.Value.ContentType);
        }

        [HttpGet("click/{id:int}")]
        public async Task<IActionResult> Click(int id)
        {
            var ad = await _context.Ads.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            var target = SafeTarget(ad?.ClickUrl);
            if (ad == null || target == null) return Redirect("/");
            _ads.CountClick(ad.Id);
            Response.Headers["Cache-Control"] = "no-store";
            return Redirect(target);
        }

        /// <summary>A site path (/credits) or an http(s) address; anything else (javascript:, //host) is refused.</summary>
        public static string SafeTarget(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            url = url.Trim();
            if (url.StartsWith("/") && !url.StartsWith("//") && !url.StartsWith("/\\")) return url;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) return uri.ToString();
            return null;
        }
    }
}
