using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Models;
using KeplerCMS.Data.Models;
using System.Collections;
using System.Collections.Generic;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class HobbaController : Controller
    {
        private readonly ICommandQueueService _commandQueueService;
        private readonly IUserService _userService;

        public HobbaController(ICommandQueueService commandQueueService, IUserService userService)
        {
            _commandQueueService = commandQueueService;
            _userService = userService;
        }

        [HousekeepingFilter(Fuse.fuse_kick)]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HousekeepingFilter(Fuse.room_alert)]
        public IActionResult Alert()
        {
            return View(new AlertViewModel { Username = "", Message = "", SuccessMessage = null});
        }
        
        [HousekeepingFilter(Fuse.room_alert)]
        [HttpPost]
        public async Task<IActionResult> Alert(AlertViewModel model)
        {
            _commandQueueService.QueueCommand(KeplerCMS.Models.Enums.CommandQueueType.remote_alert, new CommandTemplate { Message = model.Message, Users = new [] { model.Username } });

            if(ModelState.IsValid)
            {
                var dbUser = await _userService.GetUserByUsername(model.Username);
                if (dbUser == null)
                {
                    ModelState.AddModelError("Username", "Username not found");
                    return View(model);
                }
                model.SuccessMessage = "Gave alert to " + model.Username;
            }
            
            return View(model);
        }
    }
}
