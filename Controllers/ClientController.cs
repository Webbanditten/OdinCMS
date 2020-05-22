using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace KeplerCMS.Controllers
{
    public class ClientController : Controller
    {
        private IUserService _userService;
        public ClientController(IUserService userService)
        {
            _userService = userService;
        }
        [LoggedInFilter]
        public async Task<IActionResult> Index()
        {
            var userWithSSO = await _userService.GenerateSSO(int.Parse(User.Identity.Name));
            ViewData["sso"] = userWithSSO.SSOTicket;
            return View();
        }


    }
}
