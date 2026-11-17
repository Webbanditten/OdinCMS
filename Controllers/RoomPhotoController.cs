using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Controllers
{
    /// <summary>Room photos taken in housekeeping (room → Take photo), for featured rooms, news and page boxes.</summary>
    public class RoomPhotoController : Controller
    {
        private readonly DataContext _context;

        public RoomPhotoController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("room-photo/{roomId:int}.png")]
        public async Task<IActionResult> Get(int roomId)
        {
            var photo = await _context.RoomPhotos.AsNoTracking().Where(p => p.RoomId == roomId)
                .Select(p => new { p.Photo, p.TakenAt }).FirstOrDefaultAsync();
            if (photo == null) return NotFound();

            var etag = "\"" + photo.TakenAt.Ticks.ToString("x") + "\"";
            // Links from housekeeping carry ?v=<taken at>; a plain link sees a new photo within 5 minutes.
            Response.Headers.CacheControl = Request.Query.ContainsKey("v") ? "public, max-age=31536000, immutable" : "public, max-age=300";
            Response.Headers.ETag = etag;
            if (Request.Headers.IfNoneMatch.ToString() == etag) return StatusCode(304);
            return File(photo.Photo, "image/png");
        }
    }
}
