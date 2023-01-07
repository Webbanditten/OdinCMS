using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;

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
            _commandQueueService.QueueCommand(CommandQueueType.remote_alert, new CommandTemplate { Message = model.Message, Users = new [] { model.Username } });

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
        
        
        [HousekeepingFilter(Fuse.fuse_ban)]
        public IActionResult Ban()
        {
            return View(new BanViewModel { Username = "", Message = "", SuccessMessage = null, BanLength = 0, ExtraInfo = ""});
        }
        
        [HousekeepingFilter(Fuse.fuse_ban)]
        [HttpPost]
        public async Task<IActionResult> Ban(BanViewModel model)
        {
            var isBan = (model.Action == "Ban");

            if(ModelState.IsValid)
            {
                var dbUser = await _userService.GetUserByUsername(model.Username);
                if (dbUser == null)
                {
                    ModelState.AddModelError("Username", "Username not found");
                    return View(model);
                }

                var commandTemplate = new CommandTemplate
                {
                    Message = model.Message,
                    Users = new[] { model.Username },
                    BanLength = model.BanLength,
                    ExtraInfo = model.ExtraInfo,
                    BanIp = model.BanIP,
                    BanMachine = model.BanMachine,
                    UserId = (await _userService.GetUserById(int.Parse(HttpContext.User.Identity.Name))).Id
                };
                
                if (isBan)
                {
                    _commandQueueService.QueueCommand(CommandQueueType.remote_ban, commandTemplate);
                    model.SuccessMessage = "Banned " + model.Username;
                }
                else
                {
                    _commandQueueService.QueueCommand(CommandQueueType.remote_kick, commandTemplate);
                    model.SuccessMessage = "Kicked " + model.Username;
                }
            }
            
            return View(model);
        }

        [HousekeepingFilter(Fuse.fuse_ban)]
        public async Task<IActionResult> BanList(string search = null, int currentPage = 1, string letter = null, string Message = null)
        {
            ViewBag.Message = Message;
            const int pageSize = 25;
            var take = pageSize;
            var skip = (currentPage - 1) * pageSize;
            var searchResult = await _userService.BanSearch(search, take, skip, letter);
            var model = new BansViewModel
            {
                Bans = searchResult.Bans,
                Search = search,
                Letter = letter,
                CurrentPage = currentPage,
                TotalPages = searchResult.TotalResults / pageSize
            };
            return View(model);
        }

        public async Task<IActionResult> Unban(int id)
        {
            var ban = await _userService.GetBan(id);
            if (ban == null)
            {
                return RedirectToAction("BanList");
            }
            await _userService.RemoveBan(ban);
            return RedirectToAction("BanList", new { Message = "Unbanned " + ban.Username });
        }
    }
}
