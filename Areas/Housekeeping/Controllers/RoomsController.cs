using System;
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
    public class RoomsController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly IUserService _userService;
        private readonly IRoomChatlogsService _roomChatlogsService;
        public RoomsController(IRoomService roomService, IUserService userService, IRoomChatlogsService roomChatlogsService)
        {
            _roomService = roomService;
            _userService = userService;
            _roomChatlogsService = roomChatlogsService;
        }

        [HousekeepingFilter(Fuse.fuse_private_rooms)]
        public async Task<IActionResult> Index(string search = null, int currentPage = 1)
        {
            if(search == null)
            {
                return View(new SearchRoomsViewModel
                {
                    Rooms  = Enumerable.Empty<Rooms>(),
                    Search = search,
                    CurrentPage = currentPage,
                    TotalPages = 0
                });
            }
            const int pageSize = 25;
            var skip = (currentPage - 1) * pageSize;
            var searchResult = await _roomService.Search(search, pageSize, skip);
            var model = new SearchRoomsViewModel
            {
                Rooms = searchResult.Rooms,
                Search = search,
                CurrentPage = currentPage,
                TotalPages = searchResult.TotalResults / pageSize
            };
            return View(model);
        }
        [HttpPost]
        [HousekeepingFilter(Fuse.fuse_private_rooms)]
        public async Task<IActionResult> UpdateRoom([FromBody] RoomsUpdateModel model)
        {
            if (!ModelState.IsValid) return Content("error");
            
            var result = await _roomService.UpdateRoom(model);
            return Content(result);
        }

        [HousekeepingFilter(Fuse.fuse_private_rooms)]
        public async Task<IActionResult> View(int id)
        {
            var room = await _roomService.GetRoom(id);
            if (room == null) return NotFound();
            
            room.Owner = await _userService.GetUserById(room.OwnerId);
            
            return View(room);
        }
        
        [HousekeepingFilter(Fuse.fuse_see_chat_log_link)]
        public async Task<IActionResult> Chatlogs(int? userId, int? roomId, int? timestampLimiter, int take = 25, int skip = 0)
        {
            var currentTimestamp = timestampLimiter ?? (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (userId != null)
            {
                var chatlogs = await _roomChatlogsService.GetByUserId(userId.Value,currentTimestamp, take, skip);
                return Json(chatlogs);
            }
            if (roomId != null)
            {
                var chatlogs = await _roomChatlogsService.GetByRoomId(roomId.Value,currentTimestamp, take, skip);
                return Json(chatlogs);
            }
            
            return BadRequest();
        }
        
    }

}

