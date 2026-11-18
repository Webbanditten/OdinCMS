using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// What the site's edit mode (wwwroot/js/cms-edit.js) does besides opening the housekeeping editors: move, hide and
    /// delete boxes, and render a box from unsaved editor content for the live preview.
    /// </summary>
    [Area("Housekeeping")]
    public class PageEditController : Controller
    {
        private readonly DataContext _context;

        public PageEditController(DataContext context)
        {
            _context = context;
        }

        /// <summary>Columns of a page design (Views/Shared/PageDesigns).</summary>
        public static int Columns(string design) => design switch { "two_small_one_wide" => 3, "habbowood" => 4, "one_wide" => 1, _ => 2 };

        /// <param name="direction">up, down, left or right.</param>
        [HttpPost]
        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> Move(int id, string direction)
        {
            var box = await _context.Containers.FirstOrDefaultAsync(c => c.Id == id);
            if (box == null) return NotFound(new { error = "No such box" });
            var design = await _context.Pages.Where(p => p.Id == box.PageId).Select(p => p.Design).FirstOrDefaultAsync();
            var all = await _context.Containers.Where(c => c.PageId == box.PageId).ToListAsync();
            List<Containers> Column(int column) => all.Where(c => c.Column == column).OrderBy(c => c.Order).ThenBy(c => c.Id).ToList();

            var source = Column(box.Column);
            var index = source.IndexOf(box);
            switch (direction)
            {
                case "up" when index > 0:
                    source.RemoveAt(index);
                    source.Insert(index - 1, box);
                    break;
                case "down" when index < source.Count - 1:
                    source.RemoveAt(index);
                    source.Insert(index + 1, box);
                    break;
                case "left" or "right":
                    var target = box.Column + (direction == "left" ? -1 : 1);
                    if (target < 1 || target > Columns(design)) return Json(new { moved = false });
                    source.Remove(box);
                    var destination = Column(target);
                    destination.Insert(Math.Min(index, destination.Count), box);
                    box.Column = target;
                    Renumber(destination);
                    break;
                default:
                    return Json(new { moved = false });
            }
            Renumber(source);
            await _context.SaveChangesAsync();
            return Json(new { moved = true });
        }

        private static void Renumber(List<Containers> column)
        {
            for (var i = 0; i < column.Count; i++) column[i].Order = i;
        }

        [HttpPost]
        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> Toggle(int id)
        {
            var box = await _context.Containers.FirstOrDefaultAsync(c => c.Id == id);
            if (box == null) return NotFound(new { error = "No such box" });
            box.IHidden = box.IHidden == 0 ? 1 : 0;
            await _context.SaveChangesAsync();
            return Json(new { hidden = box.IHidden == 1 });
        }

        [HttpPost]
        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> Delete(int id)
        {
            var box = await _context.Containers.FirstOrDefaultAsync(c => c.Id == id);
            if (box == null) return NotFound(new { error = "No such box" });
            _context.Containers.Remove(box);
            await _context.SaveChangesAsync();
            return Json(new { deleted = true });
        }

        /// <summary>A box rendered as on the site from what is in the editor, before it is saved.</summary>
        [HttpPost]
        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public IActionResult Preview(int id, int pageId, string title, string text, string theme, string type)
        {
            var box = new Containers
            {
                Id = id, PageId = pageId, Title = title ?? "", Text = text ?? "", Theme = string.IsNullOrWhiteSpace(theme) ? "v2box-blue-light" : theme,
                Type = type ?? "container",
            };
            return PartialView("~/Views/Shared/Containers/Container.cshtml", box);
        }
    }
}
