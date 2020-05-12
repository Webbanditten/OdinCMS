using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class PromoController : Controller
    {
        private readonly IPromoService _promoService;

        public PromoController(IPromoService promoService)
        {
            _promoService = promoService;
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> Index(int pageId)
        {
            return View(await _promoService.GetPromos(pageId));
        }


        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public IActionResult Create()
        {
            return View();
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        [HttpPost]
        public async Task<IActionResult> Create(Promo model)
        {
            if (ModelState.IsValid)
            {
                await _promoService.Add(model);
                return RedirectToAction("Index", "Promo", new { message = "Promo uploaded" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> Update(int id)
        {
            var upload = await _promoService.Get(id);
            return View(upload);
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        [HttpPost]
        public async Task<IActionResult> Update(Promo model)
        {
            if (ModelState.IsValid)
            {
                await _promoService.Update(model);
                return RedirectToAction("Index", "Promo", new { message = "Promo edited", pageId = model.PageId });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> Remove(int id, int pageId)
        {
            await _promoService.Remove(id);
            return RedirectToAction("Index", "Promo", new { message = "Promo Removed", pageId = pageId });
        }
    }
}
