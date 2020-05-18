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
    public class GuestbookController : Controller
    {
        public readonly IHomeService _homeService;
        public readonly IUserService _userService;
        public GuestbookController(IHomeService homeService, IUserService userService)
        {
            _homeService = homeService;
            _userService = userService;
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Add(int ownerId, string message, int scope, string query, int widgetId)
        {
            var widget = await _homeService.GetItem(widgetId);
            if (widget != null)
            {
                var entry = await _homeService.AddGuestbookEntry(widget.Item.HomeId, message, int.Parse(User.Identity.Name));
                return View("~/Areas/MyHabbo/Views/Guestbook/Add.cshtml", entry);
            }
            return Content("ERROR");
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Preview(int ownerId, string message, int scope, string query, int widgetId)
        {

            return View(new GuestbookEntry { Entry = new HomesGuestbook { Message = message }, User = await _userService.GetUserById(User.Identity.Name) });
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Remove(int entryId, int widgetId)
        {
            await _homeService.DeleteGuestbookEntry(entryId, int.Parse(User.Identity.Name));
            var item = await _homeService.GetItem(widgetId);
            item.WidgetData = await _homeService.GetWidgetData(item.Item.OwnerId);
            return View("~/Areas/MyHabbo/Views/Items/Widget.cshtml", item);
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Configure(int widgetId)
        {
            await _homeService.ConfigureGuestbook(widgetId);
            Response.ContentType = "text/javascript";
            return View();
        }


    }
}