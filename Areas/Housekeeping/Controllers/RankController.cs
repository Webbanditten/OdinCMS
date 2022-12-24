using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using System.Linq;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class RankController : Controller
    {
        private IFuseService _fuseService;
        private IUserService _userService;

        public RankController(IFuseService fuseService, IUserService userService)
        {
            _userService = userService;
            _fuseService = fuseService;
        }

        [HousekeepingFilter(Fuse.housekeeping_ranks)]
        public async Task<IActionResult> Index(string message)
        {
            return View(new RanksViewModel
            {
                Message = message,
                Ranks = await _fuseService.GetRanks()
            });
        }

        [HousekeepingFilter(Fuse.housekeeping_ranks)]
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

        [HousekeepingFilter(Fuse.housekeeping_ranks)]
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
                    Selected = (f.UserGroup == Data.Models.FuseUserGroup.ANYONE || (data.RankRights != null && data.RankRights.Contains(f.Name) ? true : false)) ? true : false
                })
            };

            var rank = await _fuseService.CreateRank(
                new Rank { Name = data.Name}, 
                model.Fuses.Where(f => f.Selected).Select(f => new RankRights { FuseName = f.Name })
            );
            return RedirectToAction("Index", "Rank", new { message = "Rank created" });
        }

        [HousekeepingFilter(Fuse.housekeeping_ranks)]
        public async Task<IActionResult> Update(int id)
        {
            var rank = await _fuseService.GetRankById(id);
            if(rank == null) return NotFound();
            rank.RankRights = await _fuseService.GetRankRightsByRankId(id);
            var selectedFuses = rank.RankRights != null ? rank.RankRights.Select(r => r.FuseName) : new string[] { };
            var fuses = await _fuseService.GetFuses();
            var model = new RanksEditViewModel
            {
                RankBadges = await _fuseService.GetRankBadges(rank.Id),
                RankId = rank.Id,
                Name = rank.Name,
                Fuses = fuses.Select(f => new RanksSelectedFusesModel
                {
                    Name = f.Name,
                    Description = f.Description,
                    UserGroup = f.UserGroup,
                    Selected = (f.UserGroup == Data.Models.FuseUserGroup.ANYONE || selectedFuses.Contains(f.Name)) ? true : false
                })
            };
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_ranks)]
        [HttpPost]
        public async Task<IActionResult> Update([FromForm] RanksUpdatePostModel data)
        {
            var fuses = await _fuseService.GetFuses();
            var selectedFuses = fuses.Select(f => new RanksSelectedFusesModel
            {
                Name = f.Name,
                Description = f.Description,
                UserGroup = f.UserGroup,
                Selected = f.UserGroup == FuseUserGroup.ANYONE || (data.RankRights != null && data.RankRights.Any(r => r.ToLower() == f.Name.ToLower()))
            });

            await _fuseService.UpdateRank(
                new Rank { Id = data.RankId, Name = data.Name}, 
                selectedFuses.Where(f => f.Selected).Select(f => new RankRights { FuseName = f.Name, RankId = data.RankId}).ToList()
            );
            return RedirectToAction("Index", "Rank", new { message = "Rank updated" });
        }

        [HousekeepingFilter(Fuse.housekeeping_ranks)]
        public async Task<IActionResult> Remove(int id)
        {
            await _fuseService.DeleteRank(id);
            return RedirectToAction("Index", "Rank", new { message = "Rank Removed" });
        }

        [HousekeepingFilter(Fuse.housekeeping_ranks)]
        [HttpPost]
        public async Task<IActionResult> Change(int userId, int rankId, string returnUrl)
        {
            var user = await _userService.GetUserById(userId);
            if (user != null)
            {
                var rank = _fuseService.GetRankById(rankId);
                if (rank != null)
                {
                    user.Rank = rankId;
                    await _userService.Update(user);
                }
            }
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Rank", new { message = "Rank Changed" });
        }
        [HousekeepingFilter(Fuse.housekeeping_ranks)]
        [HttpPost]
        public async Task<IActionResult> AddBadge([FromBody]RankBadges badge)
        {
           var rank = _fuseService.GetRankById(badge.Rank);
            if (rank != null)
            {
                var currentBadges = await _fuseService.GetRankBadges(badge.Rank);
                if(currentBadges.Select(s=>s.Badge).Contains(badge.Badge)) {
                    return Ok("Badge already exists");
                } else {
                    await _fuseService.AddRankBadge(badge);
                }
            } else {
                return NotFound();
            }
            return Ok("Badges for rank updated");
        }
        [HousekeepingFilter(Fuse.housekeeping_ranks)]
        [HttpPost]
        public async Task<IActionResult> RemoveBadge([FromBody]RankBadges badge)
        {
            var rank = _fuseService.GetRankById(badge.Rank);
            if (rank != null)
            {
               await _fuseService.RemoveRankBadge(badge);
            } else {
                return NotFound();
            }
            return Ok("Badges for rank updated");
        }
    }
}
