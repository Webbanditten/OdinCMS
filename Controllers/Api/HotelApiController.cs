using System;
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
        private readonly IFurniService _furniService;

        public HotelApiController(ISettingsService settingsService, IUserService userService, ICommandQueueService commandQueueService, IPhotoService photoService, IFurniService furniService)
        {
            _settingService = settingsService;
            _userService = userService;
            _commandQueueService = commandQueueService;
            _photoService = photoService;
            _furniService = furniService;
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
        
        [Route("api/hotel/furni")]
        public async Task<IActionResult> GetFurni(string id)
        {
            if (id.Contains(','))
            {
                var stringIds = id.Split(',');
                var ids = new int[stringIds.Length];
                for (var i = 0; i < stringIds.Length; i++)
                {
                    ids[i] = int.Parse(stringIds[i]);
                }
                var furnis = await _furniService.Get(ids);
                return Ok(furnis);
            }
            
            var furni = await _furniService.Get(int.Parse(id));
            if (furni != null)
            {
                return Ok(furni);
            }
            return NotFound();
        }
        
        [Route("api/hotel/furni/search")]
        public async Task<IActionResult> SearchFurni(string query)
        {
            return Ok(await _furniService.Search(query));
        }
        
        
    }
}
