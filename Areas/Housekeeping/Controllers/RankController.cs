using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class RankController : Controller
    {
        private IFuseService _fuseService;

        public RankController(IFuseService fuseService)
        {
            _fuseService = fuseService;
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Index()
        {
            return View(new RanksViewModel
            {
                Fuses = await _fuseService.GetFuses(),
                Ranks = await _fuseService.GetRanks(),
                RankRights = await _fuseService.GetRankRights()
            });
        }
    }
}
