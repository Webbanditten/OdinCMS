using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Threading.Tasks;
using KeplerCMS.Services.Interfaces;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class RatingController : Controller
    {
        public readonly IHomeService _homeService;
        public RatingController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        [LoggedInFilter]
        public async Task<IActionResult> Rate(int ratingId, int givenRate)
        {
            var userId = int.Parse(User.Identity.Name);
            var homeId = int.Parse(Request.Cookies["editid"]);
            var item = await _homeService.Rate(homeId, givenRate, ratingId, userId);
            return View(item);
        }

        [Route("myhabbo/rating/reset_ratings")]
        [LoggedInFilter]
        public async Task<IActionResult> RemoveRating(int ratingId)
        {
            var userId = int.Parse(User.Identity.Name);
            var homeId = int.Parse(Request.Cookies["editid"]);
            var item = await _homeService.ResetRating(homeId, ratingId, userId);
            return View("~/Areas/MyHabbo/Views/Rating/Rate.cshtml", item);
        }
    }
}
