using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KeplerCMS.Controllers
{
    [ApiController]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class HotelApiController : Controller
    {
        private readonly ISettingsService _settingService;
        private readonly IUserService _userService;
        private readonly ICommandQueueService _commandQueueService;

        public HotelApiController(ISettingsService settingsService, IUserService userService, ICommandQueueService commandQueueService)
        {
            _settingService = settingsService;
            _userService = userService;
            _commandQueueService = commandQueueService;
        }

        [HttpGet("api/hotel/online")]
        public async Task<string> Online()
        {
            return (await _settingService.GetValue("players.online")).Value;
        }

        [HttpPost("api/hotel/habboname_exists")]
        public async Task<IActionResult> HabboNameExists(string habboName)
        {
            var user = await _userService.GetUserByUsername(habboName);
            return Content((user != null ? true : false).ToString());
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

        [HttpGet("api/hotel/convertFigure")]
        public async Task<IActionResult> ConvertFigure(string figure)
        {
            var test = Helpers.FigureHelper.ConvertFigure(figure);
            return Content(test);
        }

        [LoggedInFilter]
        [Route("components/roomNavigation")]
        public IActionResult RoomNavigation(int roomId, string roomType)
        {
            _commandQueueService.QueueCommand(Models.Enums.CommandQueueType.roomForward, $"{User.Identity.Name},{roomId},{roomType}");
            return Content("ok");
        }
    }
}
