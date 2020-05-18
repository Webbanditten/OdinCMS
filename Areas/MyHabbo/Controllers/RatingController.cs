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
            var item = await _homeService.Rate(givenRate, ratingId, userId);
            return View(item);
        }

        [Route("myhabbo/rating/reset_ratings")]
        [LoggedInFilter]
        public async Task<IActionResult> RemoveRating(int ratingId)
        {
            var userId = int.Parse(User.Identity.Name);
            var item = await _homeService.ResetRating(ratingId, userId);
            return View("~/Areas/MyHabbo/Views/Rating/Rate.cshtml", item);
        }
    }
}
