using KeplerCMS.Data;
using KeplerCMS.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Westwind.Globalization;

namespace KeplerCMS.Areas.MyHabbo
{
    /// <summary>
    /// The note and guestbook editor's link tool: finds Habbos (scope 1), rooms (2) or groups (3) by name; a click
    /// on a result inserts [habbo=id], [room=id] or [group=id] (rendered by BBCodeToString).
    /// </summary>
    [Area("MyHabbo")]
    public class LinkToolController : Controller
    {
        private const int MaxResults = 10;
        private readonly DataContext _context;

        public LinkToolController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        [LoggedInFilter]
        [Route("myhabbo/linktool/search")]
        public async Task<IActionResult> Search(string query, int scope = 1)
        {
            query = query?.Trim();
            if (string.IsNullOrEmpty(query)) return Content("");

            var (type, results) = scope switch
            {
                2 => ("room", await _context.Rooms.Where(r => r.Name.Contains(query)).OrderBy(r => r.Name).Take(MaxResults)
                        .Select(r => new { r.Id, Name = r.Name }).ToListAsync()),
                3 => ("group", await _context.Homes.Where(h => h.Type == "group" && h.GroupName.Contains(query)).OrderBy(h => h.GroupName).Take(MaxResults)
                        .Select(h => new { h.Id, Name = h.GroupName }).ToListAsync()),
                _ => ("habbo", await _context.Users.Where(u => u.Username.Contains(query)).OrderBy(u => u.Username).Take(MaxResults)
                        .Select(u => new { u.Id, Name = u.Username }).ToListAsync()),
            };

            var html = new StringBuilder("<ul>");
            var hint = DbRes.T("linktool_hint", "habbohome");
            html.Append("<li>").Append(HtmlEncoder.Default.Encode(hint == "linktool_hint" ? "Klik på et link for at sætte det ind" : hint)).Append("</li>");
            foreach (var result in results)
            {
                var name = HtmlEncoder.Default.Encode(result.Name ?? "");
                html.Append("<li><a href=\"#\" class=\"linktool-result\" type=\"").Append(type)
                    .Append("\" value=\"").Append(result.Id).Append("\" title=\"").Append(name).Append("\">")
                    .Append(name).Append("</a></li>");
            }
            html.Append("</ul>");
            return Content(html.ToString(), "text/html; charset=utf-8");
        }
    }
}
