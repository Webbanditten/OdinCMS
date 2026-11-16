using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Web;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class WidgetController : HomeEditController
    {
        private const string WidgetView = "~/Areas/MyHabbo/Views/Items/Widget.cshtml";

        public WidgetController(IHomeService homeService) : base(homeService)
        {
        }

        /// <summary>widgetId is the inventory id of the widget to place.</summary>
        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Add(int widgetId, int zIndex)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();

            var item = await HomeService.PlaceItem(homeId, widgetId, zIndex, userId, "widgets");
            if (item == null) return Error();

            SetJson("[\"" + item.Item.Id + "\"]");
            return View(WidgetView, item);
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/traxplayer/select_song")]
        public async Task<IActionResult> SelectSong(int songId, int widgetId)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();

            var item = await HomeService.SelectSong(homeId, widgetId, songId, userId);
            return item == null ? Error() : View("~/Areas/MyHabbo/Views/TraxPlayer/SelectSong.cshtml", item);
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Edit(int skinId, int widgetId)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();

            var item = await HomeService.EditItem(homeId, widgetId, skinId, userId);
            if (item == null) return Error();

            SetJson("{\"id\":\"" + item.Item.Id + "\",\"cssClass\":\"" + HttpUtility.UrlEncode(item.Item.Skin) + "\",\"type\":\"widget\"}");
            return View(WidgetView, item);
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Delete(int widgetId)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();

            return await HomeService.RemoveItem(homeId, widgetId, userId) == null ? Error() : Content("SUCCESS");
        }
    }
}
