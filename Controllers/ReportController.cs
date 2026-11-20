using System;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Controllers
{
    /// <summary>
    /// The Home's and group page's report mode ("Rapporter" in the toolbar, wwwroot/js/myhabbo/homeview.js + homeauth.js): the
    /// dialog texts and a report per object. Answers what homeauth.js expects: "SUCCESS", "SPAM" (already reported, or too many
    /// at once) or anything else for its error dialog. Staff see the reports in Housekeeping > Customer service > Website reports.
    /// </summary>
    [Route("mod")]
    public class ReportController : BaseController
    {
        /// <summary>At most this many reports per player in <see cref="BurstMinutes"/>.</summary>
        private const int BurstMax = 5;
        private const int BurstMinutes = 10;
        /// <summary>The same player reporting the same thing again within this time is SPAM (also while it is open).</summary>
        private const int RepeatHours = 24;

        private readonly DataContext _context;

        public ReportController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("localizations")]
        public IActionResult Localizations() => Json(ReportTexts.Dialogs());

        [HttpPost("add_{type}_report")]
        public async Task<IActionResult> Add(string type, int objectId)
        {
            if (!User.Identity.IsAuthenticated || !int.TryParse(User.Identity.Name, out var reporterId)) return Content("ERROR");
            if (!WebsiteReports.Types.ContainsKey(type ?? "")) return Content("ERROR");

            var found = await Find(type, objectId);
            if (found == null) return Content("ERROR");
            var (content, authorId, homeId) = found.Value;
            if (authorId == reporterId) return Content("ERROR");

            var now = DateTime.Now;
            var since = now.AddHours(-RepeatHours);
            if (await _context.WebsiteReports.AnyAsync(r => r.ReporterId == reporterId && r.Type == type && r.ObjectId == objectId
                    && (r.Status == WebsiteReports.Open || r.CreatedAt > since)))
                return Content("SPAM");
            var burst = now.AddMinutes(-BurstMinutes);
            if (await _context.WebsiteReports.CountAsync(r => r.ReporterId == reporterId && r.CreatedAt > burst) >= BurstMax)
                return Content("SPAM");

            _context.WebsiteReports.Add(new WebsiteReport
            {
                Type = type,
                ObjectId = objectId,
                HomeId = homeId,
                AuthorId = authorId,
                Content = content.Length > 4000 ? content.Substring(0, 4000) : content,
                ReporterId = reporterId,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Status = WebsiteReports.Open,
                CreatedAt = now,
            });
            await _context.SaveChangesAsync();
            return Content("SUCCESS");
        }

        /// <summary>The reported text as it is now, who wrote it and the page it is on; null when it doesn't exist.</summary>
        private async Task<(string Content, int? AuthorId, int? HomeId)?> Find(string type, int id)
        {
            switch (type)
            {
                case WebsiteReports.Name:
                case WebsiteReports.Motto:
                {
                    var user = await _context.Users.AsNoTracking().Where(u => u.Id == id).Select(u => new { u.Username, u.Motto }).FirstOrDefaultAsync();
                    if (user == null) return null;
                    return (type == WebsiteReports.Name ? user.Username : user.Motto ?? "", id, await UserHome(id));
                }
                case WebsiteReports.Stickie:
                {
                    var note = await (from i in _context.HomesItems.AsNoTracking()
                                      join d in _context.HomesItemData.AsNoTracking() on i.ItemId equals d.Id
                                      where i.Id == id && d.Type == "notes"
                                      select new { i.Data, i.OwnerId, i.HomeId }).FirstOrDefaultAsync();
                    if (note == null) return null;
                    return (note.Data ?? "", note.OwnerId, note.HomeId);
                }
                case WebsiteReports.Guestbook:
                {
                    var entry = await _context.HomesGuestbook.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
                    if (entry == null) return null;
                    return (entry.Message ?? "", entry.UserId, entry.HomeId);
                }
                case WebsiteReports.Room:
                {
                    var room = await _context.Rooms.AsNoTracking().Where(r => r.Id == id).Select(r => new { r.Name, r.Description, r.OwnerId }).FirstOrDefaultAsync();
                    if (room == null || room.OwnerId <= 0) return null;
                    return (room.Name + "\n" + (room.Description ?? ""), room.OwnerId, await UserHome(room.OwnerId));
                }
                case WebsiteReports.GroupName:
                case WebsiteReports.GroupDesc:
                {
                    var group = await _context.Homes.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id && h.Type == "group");
                    if (group == null) return null;
                    return (type == WebsiteReports.GroupName ? group.GroupName ?? "" : group.GroupDescription ?? "", group.UserId, group.Id);
                }
            }
            return null;
        }

        private Task<int?> UserHome(int userId) =>
            _context.Homes.AsNoTracking().Where(h => h.UserId == userId && h.Type == "user").Select(h => (int?)h.Id).FirstOrDefaultAsync();
    }
}
