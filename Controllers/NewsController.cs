using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Models;

namespace KeplerCMS.Controllers
{

    [MenuFilter]
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
            return View("SingleNews", new NewsViewModel { News = await _newsService.GetBySlug(slug), LatestNews = await _newsService.GetNews(0, 5) });
        }
    }
}
