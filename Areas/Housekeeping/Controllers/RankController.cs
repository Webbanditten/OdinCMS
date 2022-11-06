using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using System.Linq;

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
                Ranks = await _fuseService.GetRanks()
            });
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Create()
        {
            var fuses = await _fuseService.GetFuses();
            var model = new RanksCreateViewModel
            {
                Fuses = fuses.Select(f => new RanksSelectedFusesModel
                {
                    Name = f.Name,
                    Description = f.Description,
                    UserGroup = f.UserGroup,
                    Selected = (f.UserGroup == Data.Models.FuseUserGroup.ANYONE) ? true : false
                })
            };
            return View(model);
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] RanksCreatePostModel data)
        {
            var fuses = await _fuseService.GetFuses();
            var model = new RanksCreateViewModel
            {
                Fuses = fuses.Select(f => new RanksSelectedFusesModel
                {
                    Name = f.Name,
                    Description = f.Description,
                    UserGroup = f.UserGroup,
                    Selected = (f.UserGroup == Data.Models.FuseUserGroup.ANYONE || data.RankRights.Contains(f.Name)) ? true : false
                })
            };
            return View(model);
        }
    }
}
