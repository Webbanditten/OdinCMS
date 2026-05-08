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
        private readonly IAuditLogService _auditService;

        public NewsController(INewsService newsService, IAuditLogService auditService)
        {
            _newsService = newsService;
            _auditService = auditService;
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
                await _auditService.AddLog(AuditLogType.create_news, int.Parse(HttpContext.User.Identity.Name), 0);
                return RedirectToAction("Index", "News", new { message = "News created" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_news)]
        public async Task<IActionResult> Update(int id)
        {
            var news = await _newsService.Get(id);
            return View(news);
        }

        [HousekeepingFilter(Fuse.housekeeping_news)]
        [HttpPost]
        public async Task<IActionResult> Update(News model)
        {
            if (ModelState.IsValid)
            {
                var news = await _newsService.Update(model);
                await _auditService.AddLog(AuditLogType.edit_news, int.Parse(HttpContext.User.Identity.Name), null, null, 0, news.Id);
                return RedirectToAction("Index", "News", new { message = "News edited" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_news)]
        public async Task<IActionResult> Remove(int id)
        {
            await _newsService.Remove(id);
            await _auditService.AddLog(AuditLogType.delete_news, int.Parse(HttpContext.User.Identity.Name), null, null, 0, id);
            return RedirectToAction("Index", "News", new { message = "News Removed" });
        }
    }
}
