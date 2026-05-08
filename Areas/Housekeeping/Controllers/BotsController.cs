using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class BotsController : Controller
    {
        private readonly ICommandQueueService _commandQueueService;
        private readonly IBotService _botService;
        private readonly IRoomService _roomService;

        public BotsController(ICommandQueueService commandQueueService, IBotService botService, IRoomService roomService)
        {
            _commandQueueService = commandQueueService;
            _botService = botService;
            _roomService = roomService;
        }
        
        
        
        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> GetAllPublicRooms()
        {
            var rooms = await  _roomService.GetPublicRooms();
            
            return Json(rooms);
        }
        
        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> GetBot(int id)
        {
            var bot = await _botService.Get(id);
            
            return Json(bot);
        }
        
        
        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> UpdateBot([FromBody] Bots bot)
        {
            var success = await _botService.UpdateBot(bot);
            
            return Json(success);
        }
        
        
        [HousekeepingFilter(Fuse.fuse_bots)]
        public async Task<IActionResult> Index(string Message = null)
        {
            ViewBag.Message = Message;
            var bots = await _botService.GetAllBots();
           
            return View(new BotsListView { Bots = bots });
        }
    }
}
