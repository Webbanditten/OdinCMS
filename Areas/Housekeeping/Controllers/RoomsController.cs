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
        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
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
            return View(room);
        }
        
    }

}

