using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Models;

namespace KeplerCMS.Controllers
{
    [MaintenanceFilter]
    public class NewsController : Controller
    {
        private readonly INewsService _newsService;

        public NewsController(INewsService newsService)
        {
            _newsService = newsService;
        }

        [LoggedInFilter(false)]
        public async Task<IActionResult> Index(string slug = null)
        {
            if (slug == null)
            {
                return View(await _newsService.GetAll());
            }
            var article = await _newsService.GetBySlug(slug);
            var news = new NewsViewModel
            {
                News = article,
                LatestNews = await _newsService.GetNews(0, 5)
            };
            if(article != null)
            {
                return View("SingleNews", news);
            }

            return View("NotFound", news);
        }
    }
}
