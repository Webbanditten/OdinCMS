using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KeplerCMS.Controllers
{
    interface HabboNameRequest {
        public string habboName { get; set; }
    }
    [ApiController]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class HotelApiController : Controller
    {
        private readonly ISettingsService _settingService;
        private readonly IUserService _userService;
        private readonly ICommandQueueService _commandQueueService;
        private readonly IPhotoService _photoService;

        public HotelApiController(ISettingsService settingsService, IUserService userService, ICommandQueueService commandQueueService, IPhotoService photoService)
        {
            _settingService = settingsService;
            _userService = userService;
            _commandQueueService = commandQueueService;
            _photoService = photoService;
        }

        [HttpGet("api/hotel/online")]
        public async Task<string> Online()
        {
            return (await _settingService.Get("players.online")).Value;
        }

        [HttpPost("api/hotel/habboname_exists")]
        public async Task<IActionResult> HabboNameExists([FromForm]string habboName)
        {
            var user = await _userService.GetUserByUsername(habboName);
            return Content((user != null ? "True" : "False"));
        }

        [HttpGet("api/hotel/usersearch")]
        public async Task<IActionResult> UserSearch(string user)
        {
            var userObj = await _userService.GetUserByUsername(user);
            if(userObj != null)
            {
                return Content($"{userObj.Gender};{userObj.Figure}");
            }
            return NotFound();
        }

        [LoggedInFilter]
        [Route("components/roomNavigation")]
        public IActionResult RoomNavigation(int roomId, string roomType)
        {
            _commandQueueService.QueueCommand(Models.Enums.CommandQueueType.roomForward, new Models.CommandTemplate { UserId = int.Parse(User.Identity.Name), RoomId = roomId, RoomType = roomType });
            return Content("ok");
        }
    }
}
