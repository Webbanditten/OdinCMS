using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Threading.Tasks;
using KeplerCMS.Services.Interfaces;
using KeplerCMS.Areas.MyHabbo.Models;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class BadgeListController : Controller
    {
        public readonly IHomeService _homeService;
        public BadgeListController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        public async Task<IActionResult> BadgePaging(int pageNumber, int widgetId)
        {
            var item = await _homeService.GetItem(widgetId);
            var homeId = int.Parse(Request.Cookies["editid"]);
            item.WidgetData = await _homeService.GetWidgetData(homeId, item.Item.OwnerId);
            return View(new BadgesViewModel { Item = item, PageNumber = pageNumber });
        }
    }
}
