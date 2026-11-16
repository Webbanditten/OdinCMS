using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class StickerController : HomeEditController
    {
        public StickerController(IHomeService homeService) : base(homeService)
        {
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/sticker/place_sticker")]
        public async Task<IActionResult> PlaceSticker(int selectedStickerId, int zIndex)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();

            var item = await HomeService.PlaceItem(homeId, selectedStickerId, zIndex, userId, "stickers");
            if (item == null) return Error();

            SetJson("[\"" + item.Item.Id + "\"]");
            return View(item);
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/sticker/remove_sticker")]
        public async Task<IActionResult> RemoveSticker(int stickerId)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();

            return await HomeService.RemoveItem(homeId, stickerId, userId) == null ? Error() : Content("SUCCESS");
        }
    }
}
