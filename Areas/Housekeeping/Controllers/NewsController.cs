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
    public class NewsController : Controller
    {
        private readonly INewsService _newsService;

        public NewsController(INewsService newsService)
        {
            _newsService = newsService;
        }

        [HousekeepingFilter(Fuse.housekeeping_news)]
        public async Task<IActionResult> Index()
        {
            return View(await _newsService.GetAll());
        }


        [HousekeepingFilter(Fuse.housekeeping_news)]
        public IActionResult Create()
        {
            return View();
        }

        [HousekeepingFilter(Fuse.housekeeping_news)]
        [HttpPost]
        public async Task<IActionResult> Create(News model)
        {
            if (ModelState.IsValid)
            {
                await _newsService.Add(model);
                return RedirectToAction("Index", "News", new { message = "News created" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_news)]
        public async Task<IActionResult> Update(int id)
        {
            var upload = await _newsService.Get(id);
            return View(upload);
        }

        [HousekeepingFilter(Fuse.housekeeping_news)]
        [HttpPost]
        public async Task<IActionResult> Update(News model)
        {
            if (ModelState.IsValid)
            {
                await _newsService.Update(model);
                return RedirectToAction("Index", "News", new { message = "News edited" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_news)]
        public async Task<IActionResult> Remove(int id)
        {
            await _newsService.Remove(id);
            return RedirectToAction("Index", "News", new { message = "News Removed" });
        }
    }
}
