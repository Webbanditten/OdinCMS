using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using System.Linq;

namespace KeplerCMS.Areas.Habbowood
{
    [Area("Habbowood")]
    public class HomeController : Controller
    {
        private readonly IHabbowoodService _habbowoodService;
        public HomeController(IHabbowoodService habbowoodService)
        {
            _habbowoodService = habbowoodService;
        }

        [LoggedInFilter]
        [Route("/habbowood/")]
        public async Task<IActionResult> Index()
        {
            var movie = (await _habbowoodService.GetUsersMovies(int.Parse(User.Identity.Name))).Where(s => s.Published == false).FirstOrDefault();
            if(movie == null) {
                movie = await _habbowoodService.SaveMovie(new Data.Models.Movies { SessionId = "0000" }, int.Parse(User.Identity.Name));
            }
            return View(movie);
        }

        [LoggedInFilter]
        [Route("/habbowood/watch/{id}")]
        public async Task<IActionResult> Watch(int id)
        {
            var movie = await _habbowoodService.GetMovie(id);
            return View("Index", movie);
        }
    }
}
