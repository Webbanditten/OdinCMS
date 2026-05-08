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

        [HousekeepingFilter(Fuse.housekeeping_rewards)]
        public async Task<IActionResult> GetRewards(bool previous)
        {
            if (previous)
            {
                return Content(JsonConvert.SerializeObject(await _rewardService.GetPreviousRewards()));
            }
            return Content(JsonConvert.SerializeObject(await _rewardService.GetCurrentRewards()));
        }
        
        [HttpPost]
        [HousekeepingFilter(Fuse.housekeeping_rewards)]
        public async Task<IActionResult> CreateReward(Rewards reward)
        {
            if (ModelState.IsValid)
            {
                Response.StatusCode = 201;
                return Content(JsonConvert.SerializeObject(await _rewardService.CreateReward(reward)));
            }
            return StatusCode(400);
        }
        
        [HttpPost]
        [HousekeepingFilter(Fuse.housekeeping_rewards)]
        public async Task<IActionResult> UpdateReward(Rewards reward)
        {
            if (ModelState.IsValid)
            {
                Response.StatusCode = 204;
                return Content(JsonConvert.SerializeObject(await _rewardService.UpdateReward(reward)));
            }
            return StatusCode(400);
        }
        
        [HttpPost]
        [HousekeepingFilter(Fuse.housekeeping_rewards)]
        public async Task<IActionResult> DeleteReward(int id)
        {
            if (ModelState.IsValid)
            {
                Response.StatusCode = 201;
                return Content(JsonConvert.SerializeObject(await _rewardService.DeleteReward(id)));
            }
            return StatusCode(400);
        }



    }

}

