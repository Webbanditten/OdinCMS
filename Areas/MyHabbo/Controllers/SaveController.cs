using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class SaveController : Controller
    {
        public readonly IHomeService _homeService;
        public SaveController(IHomeService homeService)
        {
            _homeService = homeService;
        }


        [HttpPost]
        [LoggedInFilter(false)]
        public async Task<IActionResult> Index(SaveModel input)
        {
            var result = await _homeService.Save(int.Parse(User.Identity.Name), input);
            if(result)
            {
                Response.Cookies.Delete("editmode");
                return Content("<script>window.location.reload()</script>");
            } else
            {
                return Content("ERROR");
            }
        }
    }


    /* SAVE
     * 
     * 
     * id:x,y,z splitted with slashses?
     * stickienotes: 31261:352,213,222/31268:169,69,218/
        widgets: 31253:486,5,224/
        stickers: 31276:217,60,237/31277:247,74,235/31256:360,817,239/
        background: 31466:b_bg_pattern_bricks
     
     */
}
