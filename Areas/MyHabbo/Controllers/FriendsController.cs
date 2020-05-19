using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Threading.Tasks;
using KeplerCMS.Services.Interfaces;
using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Data.Models;
using Westwind.Globalization;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class FriendsController : Controller
    {
        public readonly IFriendService _friendService;
        public readonly IUserService _userService;
        public FriendsController(IFriendService friendService, IUserService userService)
        {
            _friendService = friendService;
            _userService = userService;
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Add(int accountId)
        {
            var userId = int.Parse(User.Identity.Name);
            await _friendService.AddFriend(accountId, userId);
            Response.ContentType = "application/x-javascript";
            return Content("Dialog.showInfoDialog(\"add-friend-messages\", \"" + DbRes.T("add_friend_message", "habbohome") + "\", \"" + DbRes.T("ok", "habbohome") + "\");");
        }


    }
}