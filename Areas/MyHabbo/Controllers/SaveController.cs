using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class SaveController : Controller
    {
        public SaveController()
        {
        }

        [HousekeepingFilter(Fuse.housekeeping)]
        public IActionResult Index()
        {
            return View();
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
