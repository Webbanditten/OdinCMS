using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class SaveController : HomeEditController
    {
        public SaveController(IHomeService homeService) : base(homeService)
        {
        }

        /// <summary>
        /// Saves positions and the background. The editor posts e.g.
        /// stickers=31276:217,60,237/31277:247,74,235/  widgets=31253:486,5,224/  background=31466:b_bg_pattern_bricks
        /// </summary>
        [HttpPost]
        [LoggedInFilter(false)]
        public async Task<IActionResult> Index(SaveModel input)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();
            if (!await HomeService.Save(homeId, userId, input)) return Error();

            Response.Cookies.Delete("editid");
            return Content("<script>window.location.reload()</script>");
        }
    }
}
