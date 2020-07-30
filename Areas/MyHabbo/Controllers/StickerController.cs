using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class StickerController : Controller
    {

        private readonly IHomeService _homeService;
        public StickerController(IHomeService homeService)
        {

            _homeService = homeService;
        }
        [HttpPost]
        [Route("myhabbo/sticker/place_sticker")]
        public async Task<IActionResult> PlaceSticker(int selectedStickerId, int zIndex)
        {
            var userId = int.Parse(User.Identity.Name);
            var homeId = int.Parse(Request.Cookies["editid"]);
            if(await _homeService.CanEditHome(homeId, userId))
            {
                var item = await _homeService.PlaceItem(homeId, selectedStickerId, zIndex, userId);
                Response.Headers.Add("x-json", "[\"" + item.Item.Id + "\"]");
                return View(item);
            }
            return Content("ERROR");
        }

        [HttpPost]
        [Route("myhabbo/sticker/remove_sticker")]
        public async Task<IActionResult> RemoveSticker(int stickerId)
        {
            var userId = int.Parse(User.Identity.Name);
            var homeId = int.Parse(Request.Cookies["editid"]);
            await _homeService.RemoveItem(homeId, stickerId, userId);
            return Content("SUCCESS");
        }
    }



    /////////////// place_sticker
    /// selectedStickerId: 31276, zindex: 217
    /// 

    ////////////// remove_sticker
    /// stickerId: 31257
}
