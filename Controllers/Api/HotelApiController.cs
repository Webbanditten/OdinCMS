using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using KeplerCMS.Helpers;
using KeplerCMS.Services.Implementations;

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
            
            var allowedChars = await _settingService.Get("allowed_username_chars", "1234567890qwertyuiopasdfghjklzxcvbnm-=?!@:.æøå,+<>_");
            var regexPattern = RegexHelper.RegexSafeString(allowedChars.Value); 
            var r = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var m = r.Match(habboName);
            if(m.Success)
            {
                var user = await _userService.GetUserByUsername(habboName);
                return Content((user != null ? "1" : "0"));
            }
            return Content("2");
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
