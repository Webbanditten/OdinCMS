using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KeplerCMS.Controllers
{
    [Route("api/hotel")]
    public class HotelApiController : Controller
    {
        private readonly ISettingsService _settingService;
        private readonly IUserService _userService;

        public HotelApiController(ISettingsService settingsService, IUserService userService)
        {
            _settingService = settingsService;
            _userService = userService;
        }

        [HttpGet("online")]
        [ResponseCache(Duration = 0)]
        public async Task<string> Online()
        {
            return (await _settingService.GetValue("players.online")).Value;
        }

        [HttpPost("habboname_exists")]
        public async Task<IActionResult> HabboNameExists(string habboName)
        {
                    var user = await _userService.GetUserByUsername(habboName);
            return Content((user != null ? true : false).ToString());
        }
    }
}
