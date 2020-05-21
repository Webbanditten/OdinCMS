using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KeplerCMS.Controllers
{
    [Route("api/hotel")]
    [ApiController]
    [Authorize]
    public class HotelApiController : ControllerBase
    {
        private readonly ISettingsService _settingService;

        public HotelApiController(ISettingsService settingsService)
        {
            _settingService = settingsService;
        }

        [HttpGet("online")]
        public async Task<string> Online()
        {
            return (await _settingService.GetValue("players.online")).Value;
        }
    }
}
