using System;
using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Models;
using KeplerCMS.Data.Models;
using System.Linq;
using KeplerCMS.Data;
using KeplerCMS.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class RoomsController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly IUserService _userService;
        private readonly IRoomChatlogsService _roomChatlogsService;
        private readonly IRoomPhotographer _photographer;
        private readonly IAuditLogService _audit;
        private readonly DataContext _context;
        public RoomsController(IRoomService roomService, IUserService userService, IRoomChatlogsService roomChatlogsService,
            IRoomPhotographer photographer, IAuditLogService audit, DataContext context)
        {
            _roomService = roomService;
            _userService = userService;
            _roomChatlogsService = roomChatlogsService;
            _photographer = photographer;
            _audit = audit;
            _context = context;
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
                TotalPages = (searchResult.TotalResults + pageSize - 1) / pageSize
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
            ViewBag.Photo = await _context.RoomPhotos.AsNoTracking().Where(p => p.RoomId == id)
                .Select(p => new RoomPhoto { RoomId = p.RoomId, Width = p.Width, Height = p.Height, TakenAt = p.TakenAt, TakenBy = p.TakenBy })
                .FirstOrDefaultAsync();
            ViewBag.PhotographerEnabled = _photographer.Enabled;

            return View(room);
        }

        /// <summary>Queues a photo of the room (no Habbos, pets, chat or interface) with the room photographer.</summary>
        [HttpPost]
        [HousekeepingFilter(Fuse.fuse_private_rooms)]
        public async Task<IActionResult> TakePhoto(int id)
        {
            if (!_photographer.Enabled) return Json(new { status = "failed", error = "The room photographer isn't configured" });
            var room = await _roomService.GetRoom(id);
            if (room == null) return NotFound();

            var request = _photographer.Request(room.Id, room.OwnerId == 0, int.Parse(User.Identity.Name));
            await _audit.AddLog(AuditLogType.take_room_photo, int.Parse(User.Identity.Name), room.Id.ToString(), room.Name);
            return Json(new { id = request.Id, status = request.Status });
        }

        [HousekeepingFilter(Fuse.fuse_private_rooms)]
        public async Task<IActionResult> PhotoStatus(Guid id)
        {
            var request = _photographer.Get(id);
            if (request == null) return Json(new { status = "failed", error = "Unknown request (the CMS restarted?)" });
            if (request.Status != "done") return Json(new { status = request.Status, error = request.Error });

            var photo = await _context.RoomPhotos.AsNoTracking().Where(p => p.RoomId == request.RoomId)
                .Select(p => new { p.Width, p.Height, p.TakenAt }).FirstOrDefaultAsync();
            return Json(new
            {
                status = "done",
                url = $"/room-photo/{request.RoomId}.png?v={photo?.TakenAt.Ticks}",
                width = photo?.Width,
                height = photo?.Height,
                takenAt = photo?.TakenAt.ToString("dd-MM-yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture),
            });
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

