using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
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

        /// <summary>
        /// Rooms for the editors' Room button ({room_ID}): by id, name or owner. Exact id first, then rooms with a photo,
        /// then by name.
        /// </summary>
        [HttpGet]
        [Route("housekeeping/editor/rooms")]
        [HousekeepingFilter(new[] { Fuse.housekeeping_news, Fuse.housekeeping_pages, Fuse.fuse_campaigns })]
        public async Task<IActionResult> Rooms(string query)
        {
            query = query?.Trim();
            if (string.IsNullOrEmpty(query) || query.Length < 2 && !int.TryParse(query, out _)) return Json(Array.Empty<object>());

            var like = "%" + query.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            var id = int.TryParse(query, out var parsed) ? parsed : 0;
            var owners = await _context.Users.Where(u => EF.Functions.Like(u.Username, like)).Select(u => u.Id).Take(MaxResults).ToListAsync();
            var rooms = await _context.Rooms.AsNoTracking()
                .Where(r => r.Id == id || EF.Functions.Like(r.Name, like) || owners.Contains(r.OwnerId))
                .OrderBy(r => r.Id == id ? 0 : 1).ThenBy(r => r.Name)
                .Take(200).ToListAsync();
            var items = await RoomItems(rooms);
            return Json(items.OrderBy(r => r.id == id ? 0 : r.photo != null ? 1 : 2).ThenBy(r => r.name).Take(MaxResults));
        }

        /// <summary>
        /// Rooms that have a photo, newest photo first: what the promo picture builder lists before anything is typed
        /// (a promo is cut from the photo).
        /// </summary>
        [HttpGet]
        [Route("housekeeping/editor/rooms/photographed")]
        [HousekeepingFilter(new[] { Fuse.housekeeping_news, Fuse.housekeeping_pages, Fuse.fuse_campaigns })]
        public async Task<IActionResult> PhotographedRooms()
        {
            var ids = await _context.RoomPhotos.AsNoTracking().OrderByDescending(p => p.TakenAt).Select(p => p.RoomId).Take(60).ToListAsync();
            var rooms = await _context.Rooms.AsNoTracking().Where(r => ids.Contains(r.Id)).ToListAsync();
            var items = await RoomItems(rooms);
            return Json(items.OrderBy(r => ids.IndexOf(r.id)));
        }

        /// <summary>One room for the dialog when the cursor is on a {room_ID} code.</summary>
        [HttpGet]
        [Route("housekeeping/editor/rooms/{id:int}")]
        [HousekeepingFilter(new[] { Fuse.housekeeping_news, Fuse.housekeeping_pages, Fuse.fuse_campaigns })]
        public async Task<IActionResult> Room(int id)
        {
            var room = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (room == null) return NotFound();
            return Json((await RoomItems(new() { room })).First());
        }

        private record RoomItem(int id, string name, string description, string owner, int accessType, string photo, int photoWidth, int photoHeight);

        private async Task<System.Collections.Generic.List<RoomItem>> RoomItems(System.Collections.Generic.List<Data.Models.Rooms> rooms)
        {
            var ids = rooms.Select(r => r.Id).ToList();
            var ownerIds = rooms.Select(r => r.OwnerId).Distinct().ToList();
            var names = await _context.Users.Where(u => ownerIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username);
            var photos = await _context.RoomPhotos.AsNoTracking().Where(p => ids.Contains(p.RoomId))
                .Select(p => new { p.RoomId, p.TakenAt, p.Width, p.Height }).ToDictionaryAsync(p => p.RoomId);
            return rooms.Select(r =>
            {
                photos.TryGetValue(r.Id, out var photo);
                return new RoomItem(r.Id, r.Name, r.Description, r.OwnerId > 0 ? names.GetValueOrDefault(r.OwnerId) : null, r.AccessType,
                    photo == null ? null : $"/room-photo/{r.Id}.png?v={photo.TakenAt.Ticks}", photo?.Width ?? 0, photo?.Height ?? 0);
            }).ToList();
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
