using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Xml;
using System.IO;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Habbowood
{
    [Area("Habbowood")]
    public class MovieController : Controller
    {
        private readonly IHabbowoodService _habbowoodService;
        public MovieController(IHabbowoodService habbowoodService)
        {
            _habbowoodService = habbowoodService;
        }

        [HttpPost]
        public async Task<IActionResult> Save(string data, int movie_id, string __app_key)
        {
            var movieName = "untitled";
            XmlReaderSettings settings = new XmlReaderSettings
            {
                IgnoreWhitespace = true
            };
            using (XmlReader reader = XmlReader.Create(new StringReader(data), settings))
            {
                while (reader.Read())
                {
                    if(reader.Name == "movie" && reader.NodeType != XmlNodeType.EndElement)
                    {
                        movieName = reader.GetAttribute("name");
                    }
                }
            }
            var dbMovie = await _habbowoodService.GetMovieBySession(__app_key);
            if(dbMovie != null)
            {
                await _habbowoodService.SaveMovie(new Data.Models.Movies { Id = dbMovie.Id, Name = movieName, Data = data }, null);
            }
            return Content($"{dbMovie.Id}");
        }

        public async Task<IActionResult> Movie(int movie_id)
        {
            Response.ContentType = "text/xml";
            var movie = await _habbowoodService.GetMovie(movie_id);
            if(movie != null && movie.Data != null)
            {
                return Content(movie.Data);
            }
            return Content("<?xml version=\"1.0\" encoding=\"UTF-8\" ?><movie>blank</movie>");
        }
        [LoggedInFilter]
        public async Task<IActionResult> Publish(int movieId)
        {
            var dbMovie = await _habbowoodService.GetMovie(movieId);
            if(dbMovie != null)
            {
                if(dbMovie.UserId == int.Parse(User.Identity.Name))
                {
                    dbMovie.Published = true;
                    await _habbowoodService.SaveMovie(dbMovie, null);
                }
            }
            return RedirectToAction("Watch", "Home", new { id = movieId });
        }

        public async Task<IActionResult> Ping()
        {
            return Content("pong");
        }
    }
}
