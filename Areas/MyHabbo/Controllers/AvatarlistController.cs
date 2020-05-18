using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Threading.Tasks;
using KeplerCMS.Services.Interfaces;
using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class AvatarlistController : Controller
    {
        public readonly IHomeService _homeService;
        public readonly IUserService _userService;
        public AvatarlistController(IHomeService homeService, IUserService userService)
        {
            _homeService = homeService;
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> AvatarInfo(int anAccountId)
        {
            var user = await _userService.GetUserById(anAccountId.ToString());
            if (user != null)
            {
                return View(user);
            }
            return Content("ERROR");
        }

        [HttpPost]
        public async Task<IActionResult> FriendSearchPaging(int pageNumber, string searchString, int widgetId)
        {
            var item = await _homeService.GetItem(widgetId);
            item.WidgetData = await _homeService.GetWidgetData(item.Item.OwnerId);
            return View(new AvatarlistViewModel { Widget = item, PageNumber = pageNumber, Search = searchString });
        }
    }
}