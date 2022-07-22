using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;

namespace KeplerCMS.Controllers
{
    [MaintenanceFilter]
    public class ClientController : Controller
    {
        private readonly IUserService _userService;


        public ClientController(IUserService userService)
        {
            _userService = userService;
        }
        [LoggedInFilter]
        public async Task<IActionResult> Index(int forwardId = 0, int roomId = 0)
        {
            var userWithSSO = await _userService.GenerateSSO(int.Parse(User.Identity.Name));
            ViewData["sso"] = userWithSSO.SSOTicket;
            ViewData["forwardId"] = forwardId;
            ViewData["roomId"] = roomId;
            return View();
        }

    }
}
