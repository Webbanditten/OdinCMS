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
    /// Hobba tools > Content moderation > Tag moderation (Sulake's "Tag moderation"): the tags on Habbo Homes and group pages
    /// (table tags: user_id for a Habbo, group_id for a group). The most used ones, a search, and delete: one tag or a word
    /// everywhere it is used.
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.fuse_receive_calls_for_help)]
    public class TagModerationController : Controller
    {
        private readonly DataContext _context;
        private readonly IAuditLogService _audit;

        public TagModerationController(DataContext context, IAuditLogService audit)
        {
            _context = context;
            _audit = audit;
        }

        public record TagRow(int Id, string Tag, int UserId, string Username, int GroupId, string GroupName);

        public class TagsPage
        {
            public string Search { get; set; }
            public List<(string Tag, int Count)> Top { get; set; }
            public List<TagRow> Tags { get; set; }
            public int Total { get; set; }
            public string Message { get; set; }
        }

        public async Task<IActionResult> Index(string search = null, string message = null)
        {
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLowerInvariant();
            var top = await _context.Tags.AsNoTracking().GroupBy(t => t.Tag).Select(g => new { g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count).ThenBy(g => g.Key).Take(60).ToListAsync();
            var page = new TagsPage
            {
                Search = search,
                Message = message,
                Total = await _context.Tags.CountAsync(),
                Top = top.Select(t => (t.Key, t.Count)).ToList(),
                Tags = new List<TagRow>(),
            };
            if (search != null)
            {
                var tags = await _context.Tags.AsNoTracking().Where(t => t.Tag.Contains(search)).OrderBy(t => t.Tag).ThenByDescending(t => t.Id).Take(300).ToListAsync();
                var userIds = tags.Where(t => t.UserId > 0).Select(t => t.UserId).Distinct().ToList();
                var groupIds = tags.Where(t => t.GroupId > 0).Select(t => t.GroupId).Distinct().ToList();
                var users = await _context.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username);
                var groups = await _context.Homes.AsNoTracking().Where(h => groupIds.Contains(h.Id)).ToDictionaryAsync(h => h.Id, h => h.GroupName);
                page.Tags = tags.Select(t => new TagRow(t.Id, t.Tag, t.UserId, users.GetValueOrDefault(t.UserId), t.GroupId, groups.GetValueOrDefault(t.GroupId))).ToList();
            }
            return View(page);
        }

        /// <summary>Deletes the checked tags, or (all = the word) every tag that is exactly that word.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int[] ids, string all = null, string search = null)
        {
            var tags = !string.IsNullOrWhiteSpace(all)
                ? await _context.Tags.Where(t => t.Tag == all.Trim().ToLower()).ToListAsync()
                : await _context.Tags.Where(t => (ids ?? new int[0]).Contains(t.Id)).ToListAsync();
            if (tags.Count == 0) return RedirectToAction("Index", new { search, message = "Nothing to delete." });

            _context.Tags.RemoveRange(tags);
            await _context.SaveChangesAsync();
            var words = string.Join(", ", tags.Select(t => t.Tag).Distinct());
            await _audit.AddLog(AuditLogType.delete_tag, int.Parse(User.Identity.Name), "Deleted " + tags.Count + (tags.Count == 1 ? " tag" : " tags"), words.Length > 250 ? words[..250] : words);
            return RedirectToAction("Index", new { search, message = "Deleted " + tags.Count + (tags.Count == 1 ? " tag" : " tags") + " (" + words + ")." });
        }
    }
}
