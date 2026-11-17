using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// Lookups for the housekeeping editors' dialogs: Habbos for the image dialogs (wwwroot/housekeeping/js/editor-images.js)
    /// and catalogue items for "Buy furni" (wwwroot/housekeeping/js/editor-shop.js).
    /// </summary>
    [Area("Housekeeping")]
    public class EditorController : Controller
    {
        private const int MaxResults = 40;
        private readonly DataContext _context;
        private readonly IFurniShopService _shop;

        public EditorController(DataContext context, IFurniShopService shop)
        {
            _context = context;
            _shop = shop;
        }

        /// <summary>
        /// Catalogue items for {buy_furni_ID}, by name, sale code or id: those any player can buy first; the others come
        /// with the reason they can't be sold on the website.
        /// </summary>
        [HttpGet]
        [Route("housekeeping/editor/shopfurni")]
        [HousekeepingFilter(new[] { Fuse.housekeeping_news, Fuse.housekeeping_pages, Fuse.fuse_campaigns })]
        public async Task<IActionResult> ShopFurni(string query)
        {
            var items = await _shop.Search(query);
            return Json(items.Select(e => new
            {
                id = e.Id,
                name = e.Name,
                description = e.Description,
                saleCode = e.SaleCode,
                sprite = e.FirstDefinition?.Sprite,
                price = e.Price,
                isPackage = e.IsPackage,
                notForSale = _shop.WhyNotForSale(e),
            }));
        }

        /// <summary>One catalogue item for the dialog when the cursor is on a {buy_furni_ID} code.</summary>
        [HttpGet]
        [Route("housekeeping/editor/shopfurni/{id:int}")]
        [HousekeepingFilter(new[] { Fuse.housekeeping_news, Fuse.housekeeping_pages, Fuse.fuse_campaigns })]
        public async Task<IActionResult> ShopFurniItem(int id)
        {
            var e = await _shop.Get(id);
            if (e == null) return NotFound();
            return Json(new { id = e.Id, name = e.Name, description = e.Description, saleCode = e.SaleCode, sprite = e.FirstDefinition?.Sprite, price = e.Price, isPackage = e.IsPackage, notForSale = _shop.WhyNotForSale(e) });
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
