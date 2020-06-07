using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Web;

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

        [HttpGet("avatarimage")]
        public async Task<IActionResult> AvatarImage()
        {
            var nameValues = HttpUtility.ParseQueryString(Request.QueryString.ToString());
            if(nameValues.Get("figure") != null)
            {
                var figure = Helpers.FigureHelper.ConvertFigure(nameValues.Get("figure"), nameValues.Get("direction") != null ? int.Parse(nameValues.Get("direction")) : 0);
                nameValues.Set("figure", figure);
            }
            return Redirect("http://www.habbo.com/habbo-imaging/avatarimage?" + nameValues.ToString());
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
