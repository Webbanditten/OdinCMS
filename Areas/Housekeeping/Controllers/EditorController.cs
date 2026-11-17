using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>Lookups for the housekeeping editors' image dialogs (wwwroot/housekeeping/js/editor-images.js).</summary>
    [Area("Housekeeping")]
    public class EditorController : Controller
    {
        private const int MaxResults = 40;
        private readonly DataContext _context;

        public EditorController(DataContext context)
        {
            _context = context;
        }

        /// <summary>Habbos whose name contains the query, with their figure: exact name first, then names starting with it.</summary>
        [HttpGet]
        [Route("housekeeping/editor/habbos")]
        [HousekeepingFilter(new[] { Fuse.housekeeping_news, Fuse.housekeeping_pages, Fuse.fuse_campaigns })]
        public async Task<IActionResult> Habbos(string query)
        {
            query = query?.Trim();
            if (string.IsNullOrEmpty(query) || query.Length < 2) return Json(Array.Empty<object>());

            // LIKE follows the column's case-insensitive collation (StartsWith compared case-sensitively).
            var like = query.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var habbos = await _context.Users
                .Where(u => EF.Functions.Like(u.Username, "%" + like + "%") && u.Figure != null && u.Figure != "")
                .OrderBy(u => u.Username == query ? 0 : EF.Functions.Like(u.Username, like + "%") ? 1 : 2)
                .ThenBy(u => u.Username)
                .Take(MaxResults)
                .Select(u => new { u.Username, u.Figure })
                .ToListAsync();
            return Json(habbos);
        }
    }
}
