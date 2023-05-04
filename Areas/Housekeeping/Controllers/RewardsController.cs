using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Models;
using KeplerCMS.Data.Models;
using System.Linq;

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
        

        
    }

}

