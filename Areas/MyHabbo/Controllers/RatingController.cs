using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class RatingController : HomeEditController
    {
        private const string RateView = "~/Areas/MyHabbo/Views/Rating/Rate.cshtml";

        public RatingController(IHomeService homeService) : base(homeService)
        {
        }

        /// <summary>Rates the home the rating widget is on (1-5, once per visitor, not your own).</summary>
        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Rate(int ratingId, int givenRate)
        {
            if (CurrentUserId is not int userId) return Error();

            var item = await HomeService.Rate(givenRate, ratingId, userId);
            return item == null ? Error() : View(RateView, item);
        }

        /// <summary>The home owner clears the home's ratings.</summary>
        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/rating/reset_ratings")]
        public async Task<IActionResult> RemoveRating(int ratingId)
        {
            if (CurrentUserId is not int userId) return Error();

            var widget = await HomeService.GetItem(ratingId);
            if (widget == null) return Error();

            var item = await HomeService.ResetRating(widget.Item.HomeId, ratingId, userId);
            return item == null ? Error() : View(RateView, item);
        }
    }
}
