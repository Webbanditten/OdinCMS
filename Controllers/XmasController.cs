using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KeplerCMS.Controllers
{
    /// <summary>
    /// The Christmas section's own actions. {xmas_home_gift}: a logged-in player fetches this Christmas's free Habbo Home
    /// decorations once (setting cms.xmas_home_gift = cms_homes_item_data ids, comma-separated; they land in the Home
    /// inventory). Only while a shown box on a shown page offers it, so the box's dates open and close it.
    /// </summary>
    public class XmasController : Controller
    {
        public const string GiftSetting = "cms.xmas_home_gift";
        private readonly DataContext _context;
        private readonly IHomeService _homes;
        private readonly ILogger<XmasController> _logger;

        public XmasController(DataContext context, IHomeService homes, ILogger<XmasController> logger)
        {
            _context = context;
            _homes = homes;
            _logger = logger;
        }

        /// <summary>The gift's key: Christmas 2026 runs into January 2027 and is still "xmas-2026" there.</summary>
        public static string GiftKey(DateTime now) => "xmas-" + (now.Month <= 6 ? now.Year - 1 : now.Year);

        /// <summary>The gift's items (cms_homes_item_data ids) from the setting, in its order.</summary>
        public static async Task<List<HomesItemData>> GiftItems(DataContext context)
        {
            var value = await context.Settings.AsNoTracking().Where(s => s.Setting == GiftSetting).Select(s => s.Value).FirstOrDefaultAsync();
            var ids = (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => int.TryParse(v, out var id) ? id : 0).Where(id => id > 0).Distinct().ToList();
            var items = await context.HomesItemData.AsNoTracking().Where(i => ids.Contains(i.Id)).ToListAsync();
            return ids.Select(id => items.FirstOrDefault(i => i.Id == id)).Where(i => i != null).ToList();
        }

        [HttpPost("xmas/home-gift")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HomeGift(string returnUrl)
        {
            var back = Url.IsLocalUrl(returnUrl) ? returnUrl : "/xmas";
            string With(string result) => back + (back.Contains('?') ? "&" : "?") + "gift=" + result + "#xmas-home-gift";
            if (User.Identity?.IsAuthenticated != true) return Redirect("/account/login");
            var userId = int.Parse(User.Identity.Name);

            var now = DateTime.Now;
            var offered = await (from box in _context.Containers
                                 join page in _context.Pages on box.PageId equals page.Id
                                 where box.Text.Contains(ContentCodes.XmasHomeGift) && box.IHidden == 0 && page.IHidden == 0
                                       && (box.StartDate == null || box.StartDate <= now) && (box.EndDate == null || box.EndDate >= now)
                                 select box.Id).AnyAsync();
            var items = await GiftItems(_context);
            if (!offered || items.Count == 0) return Redirect(With("closed"));

            // The unique (gift, player) row comes first: a second click, or two at once, can't fetch it twice.
            var key = GiftKey(now);
            if (await _context.HomeGifts.AnyAsync(g => g.GiftKey == key && g.UserId == userId)) return Redirect(With("taken"));
            _context.HomeGifts.Add(new HomeGift { GiftKey = key, UserId = userId, ClaimedAt = now });
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Redirect(With("taken"));
            }
            foreach (var item in items)
            {
                // A background the player already has is skipped (a Home keeps one of each).
                if (!await _homes.GiveItem(item.Id, 1, userId)) _logger.LogInformation("Xmas gift: {User} already has {Item}", userId, item.CssClass);
            }
            return Redirect(With("ok"));
        }
    }
}
