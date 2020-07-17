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

        private readonly IHomeService _homeService;
        public TagController(IHomeService homeService)
        {
            _homeService = homeService;
        }
        [HttpPost]
        [Route("myhabbo/tag/add")]
        public async Task<IActionResult> Add(int accountId, string tagName)
        {

            var userId = int.Parse(User.Identity.Name);
            /*
                invalidtag, taglimit, valid
            */
            return Content("taglimit");
        }

        [HttpPost]
        [Route("myhabbo/tag/remove")]
        public async Task<IActionResult> Remove(int accountId, string tagName)
        {
            var userId = int.Parse(User.Identity.Name);
            return View("~/Areas/MyHabbo/Views/Tag/List.cshtml");
        }

        [Route("myhabbo/tag/list")]
        public async Task<IActionResult> List(string tagMsgCode, int accountId)
        {
            var userId = int.Parse(User.Identity.Name);
            return View("~/Areas/MyHabbo/Views/Tag/List.cshtml");
        }
    }



    /////////////// place_sticker
    /// selectedStickerId: 31276, zindex: 217
    /// 

    ////////////// remove_sticker
    /// stickerId: 31257
}
