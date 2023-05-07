using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Models;
using KeplerCMS.Data.Models;
using System.Linq;
using Newtonsoft.Json;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class RewardsController : Controller
    {
        private readonly IRewardService _rewardService;
        public RewardsController(IRewardService rewardService)
        {
            _rewardService = rewardService;
        }

        [HousekeepingFilter(Fuse.housekeeping_rewards)]
        public async Task<IActionResult> Index(bool previousRewards)
        {
           
            return View();
        }

        public async Task<IActionResult> GetRewards(bool previous)
        {
            if (previous)
            {
                return Content(JsonConvert.SerializeObject(await _rewardService.GetPreviousRewards()));
            }
            return Content(JsonConvert.SerializeObject(await _rewardService.GetCurrentRewards()));
        }



    }

}

