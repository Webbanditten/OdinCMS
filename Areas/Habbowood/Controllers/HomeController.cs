using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Services.Implementations;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Areas.Habbowood
{
    /// <summary>
    /// The Habbowood pages in the site layout, like habbo.com's 2007 ones (the film strip menu and background,
    /// styles/habbowood.css): the studio with the movie editor (/habbowood), the page after saving (/habbowood/end), the
    /// player's own movies (/habbowood/mine) and a movie with its rating (/habbowood/watch/{id}). The editor and player
    /// load and save through MovieController; the main page is the CMS page entertainment/habbowood.
    /// </summary>
    [Area("Habbowood")]
    public class HomeController : Controller
    {
        private readonly IHabbowoodService _habbowoodService;
        private readonly DataContext _context;

        public HomeController(IHabbowoodService habbowoodService, DataContext context)
        {
            _habbowoodService = habbowoodService;
            _context = context;
        }

        private int UserId => int.Parse(User.Identity.Name);

        [LoggedInFilter]
        [Route("/habbowood/")]
        public async Task<IActionResult> Index(int? movie)
        {
            var draft = await _habbowoodService.GetDraft(UserId, movie);
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == UserId);
            return View("Studio", new StudioViewModel { Movie = draft, Username = user?.Username, HabboClub = user?.HasHabboClub == true });
        }

        /// <summary>The editor's end_page_url: it goes here after the movie is saved (with the movie's id, or the latest one).</summary>
        [LoggedInFilter]
        [Route("/habbowood/end")]
        public async Task<IActionResult> End(int? movieId, int? movie_id)
        {
            var id = movieId ?? movie_id;
            var movies = _context.Movies.AsNoTracking().Where(m => m.UserId == UserId && m.Data != null && m.Data != "");
            var movie = id != null ? await movies.FirstOrDefaultAsync(m => m.Id == id)
                : await movies.Where(m => m.IPublished == 0).OrderByDescending(m => m.Id).FirstOrDefaultAsync();
            if (movie == null) return RedirectToAction("Mine");
            return View("End", new EndViewModel { Movie = movie, Subtitle = HabbowoodService.MovieAttribute(movie.Data, "subtitle") });
        }

        [LoggedInFilter]
        [Route("/habbowood/mine")]
        public async Task<IActionResult> Mine()
        {
            return View("Mine", await _habbowoodService.GetUserMovieList(UserId));
        }

        /// <summary>Anyone may watch a published movie; an unpublished one only its director (as a preview).</summary>
        [LoggedInFilter(false)]
        [Route("/habbowood/watch/{id}")]
        public async Task<IActionResult> Watch(int id)
        {
            var movie = await _habbowoodService.GetMovie(id);
            var loggedIn = User.Identity?.IsAuthenticated == true;
            var isOwner = loggedIn && movie != null && movie.UserId == UserId;
            if (movie == null || string.IsNullOrEmpty(movie.Data) || (!movie.Published && !isOwner)) return NotFound();

            var votes = await _context.MovieVotes.AsNoTracking().Where(v => v.MovieId == id).Select(v => v.Rating).ToListAsync();
            var director = await _context.Users.AsNoTracking().Where(u => u.Id == movie.UserId).Select(u => u.Username).FirstOrDefaultAsync();
            return View("Watch", new WatchViewModel
            {
                Movie = movie,
                Subtitle = HabbowoodService.MovieAttribute(movie.Data, "subtitle"),
                Director = director,
                Rating = votes.Count > 0 ? votes.Average() : 0,
                Votes = votes.Count,
                LoggedIn = loggedIn,
                CanVote = loggedIn && movie.Published && !isOwner && await _habbowoodService.CanVote(id, UserId),
                IsOwner = isOwner,
                Top = await _habbowoodService.GetMovieList("top", 5),
            });
        }

        [LoggedInFilter]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/habbowood/publish")]
        public async Task<IActionResult> Publish(int movieId)
        {
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == movieId && m.UserId == UserId);
            if (movie == null || string.IsNullOrEmpty(movie.Data)) return RedirectToAction("Mine");
            movie.Published = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("Watch", new { id = movieId });
        }

        [LoggedInFilter]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("/habbowood/delete")]
        public async Task<IActionResult> Delete(int movieId)
        {
            await _habbowoodService.DeleteMovie(movieId, UserId);
            return RedirectToAction("Mine");
        }
    }
}
