using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// Hobba tools > Content moderation > Stickie viewer (Sulake's): every note on a Habbo's Home or group page, the notes a
    /// Habbo wrote, or notes containing a text, whether reported or not; delete the checked ones.
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.fuse_receive_calls_for_help)]
    public class StickieViewerController : Controller
    {
        private readonly DataContext _context;
        private readonly IAuditLogService _audit;

        public StickieViewerController(DataContext context, IAuditLogService audit)
        {
            _context = context;
            _audit = audit;
        }

        public record NoteRow(int Id, int HomeId, string HomeType, int HomeOwnerId, string HomeName, int WriterId, string Writer, string Text, string Skin);

        public class NotesPage
        {
            public string Criteria { get; set; }
            public string Query { get; set; }
            public List<NoteRow> Notes { get; set; } = new();
            public string Message { get; set; }
        }

        public async Task<IActionResult> Index(string criteria = "home", string query = null, string message = null)
        {
            var page = new NotesPage { Criteria = criteria, Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim(), Message = message };
            if (page.Query == null) return View(page);

            var notes = from i in _context.HomesItems.AsNoTracking()
                        join d in _context.HomesItemData.AsNoTracking() on i.ItemId equals d.Id
                        join h in _context.Homes.AsNoTracking() on i.HomeId equals h.Id
                        where d.Type == "notes"
                        select new { i.Id, i.HomeId, i.OwnerId, i.Data, i.Skin, h.Type, h.UserId, h.GroupName };
            switch (criteria)
            {
                case "writer":
                {
                    var userId = await UserId(page.Query);
                    notes = notes.Where(n => n.OwnerId == userId);
                    break;
                }
                case "text":
                    if (page.Query.Length < 3) { page.Message = "Search for at least 3 letters."; return View(page); }
                    notes = notes.Where(n => n.Data.Contains(page.Query));
                    break;
                default:
                {
                    page.Criteria = "home";
                    var userId = await UserId(page.Query);
                    notes = notes.Where(n => n.Type == "user" && n.UserId == userId);
                    break;
                }
            }
            var found = await notes.OrderByDescending(n => n.Id).Take(200).ToListAsync();
            var ids = found.SelectMany(n => new[] { n.OwnerId, n.UserId }).Distinct().ToList();
            var names = await _context.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username);
            page.Notes = found.Select(n => new NoteRow(n.Id, n.HomeId, n.Type, n.UserId, n.Type == "group" ? n.GroupName : names.GetValueOrDefault(n.UserId),
                n.OwnerId, names.GetValueOrDefault(n.OwnerId), n.Data ?? "", n.Skin)).ToList();
            return View(page);
        }

        private async Task<int> UserId(string username) =>
            await _context.Users.AsNoTracking().Where(u => u.Username == username).Select(u => u.Id).FirstOrDefaultAsync();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int[] ids, string criteria = null, string query = null)
        {
            ids ??= new int[0];
            var notes = await (from i in _context.HomesItems
                               join d in _context.HomesItemData on i.ItemId equals d.Id
                               where ids.Contains(i.Id) && d.Type == "notes"
                               select i).ToListAsync();
            if (notes.Count == 0) return RedirectToAction("Index", new { criteria, query, message = "Check the notes to delete first." });

            _context.HomesItems.RemoveRange(notes);
            await _context.SaveChangesAsync();
            foreach (var note in notes)
            {
                var text = note.Data ?? "";
                await _audit.AddLog(AuditLogType.delete_stickie, int.Parse(User.Identity.Name), "Deleted note #" + note.Id + " on Home #" + note.HomeId,
                    text.Length > 250 ? text[..250] : text, note.OwnerId, note.Id);
            }
            return RedirectToAction("Index", new { criteria, query, message = "Deleted " + notes.Count + (notes.Count == 1 ? " note." : " notes.") });
        }
    }
}
