using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class GuestbookController : HomeEditController
    {
        private readonly IUserService _userService;

        public GuestbookController(IHomeService homeService, IUserService userService) : base(homeService)
        {
            _userService = userService;
        }

        /// <summary>Writes in the guestbook of the widget's home (a private one only for owner, editors, friends or members).</summary>
        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Add(string message, int widgetId)
        {
            if (CurrentUserId is not int userId) return Error();

            var widget = await HomeService.GetItem(widgetId);
            if (widget == null) return Error();

            var entry = await HomeService.AddGuestbookEntry(widget.Item.HomeId, message, userId);
            return entry == null ? Error() : View("~/Areas/MyHabbo/Views/Guestbook/Add.cshtml", entry);
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Preview(string message)
        {
            if (CurrentUserId is not int userId) return Error();

            return View(new GuestbookEntry { Entry = new HomesGuestbook { Message = message, Timestamp = DateTime.Now }, User = await _userService.GetUserById(userId) });
        }

        /// <summary>The entry's author or the home's editors remove an entry.</summary>
        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Remove(int entryId, int widgetId)
        {
            if (CurrentUserId is not int userId) return Error();

            await HomeService.DeleteGuestbookEntry(entryId, userId);
            var widget = await HomeService.GetItem(widgetId);
            if (widget == null) return Error();

            widget.WidgetData = await HomeService.GetWidgetData(widget.Item.HomeId, userId);
            return View("~/Areas/MyHabbo/Views/Items/Widget.cshtml", widget);
        }

        /// <summary>Toggles the guestbook between public and private.</summary>
        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Configure(int widgetId)
        {
            if (CurrentUserId is not int userId) return Error();
            if (await HomeService.ConfigureGuestbook(widgetId, userId) == null) return Error();

            Response.ContentType = "text/javascript";
            return View();
        }
    }
}
