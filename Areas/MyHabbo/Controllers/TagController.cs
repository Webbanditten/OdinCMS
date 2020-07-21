using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class TagController : Controller
    {

        private readonly IUserService _userService;
        public TagController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpPost]
        [Route("myhabbo/tag/add")]
        public async Task<IActionResult> Add(int accountId, string tagName)
        {
            /*
               invalidtag, taglimit, valid
            */
            if (tagName.Length <= 1 || tagName.Length > 20 || tagName.Contains("fuck"))
            {
                return Content("invalidtag");
            }

            var userId = int.Parse(User.Identity.Name);
            var tags = await _userService.Tags(userId);

            if(tags.Count >= 8) {
                return Content("taglimit");
            }

            var tag = await _userService.AddTag(new Data.Models.Tags { UserId = userId, Tag = tagName.ToLower() });

            return Content("valid");
        }

        [HttpPost]
        [Route("myhabbo/tag/remove")]
        public async Task<IActionResult> Remove(int accountId, string tagName)
        {
            var userId = int.Parse(User.Identity.Name);
            _userService.RemoveTag(new Data.Models.Tags { UserId = userId, Tag = tagName });
            var tags = await _userService.Tags(userId);
            return View("~/Areas/MyHabbo/Views/Tag/List.cshtml", tags);
        }

        [Route("myhabbo/tag/list")]
        public async Task<IActionResult> List(string tagMsgCode, int accountId)
        {
            var userId = int.Parse(User.Identity.Name);
            var tags = await _userService.Tags(userId);
            return View("~/Areas/MyHabbo/Views/Tag/List.cshtml", tags);
        }
    }



    /////////////// place_sticker
    /// selectedStickerId: 31276, zindex: 217
    /// 

    ////////////// remove_sticker
    /// stickerId: 31257
}
