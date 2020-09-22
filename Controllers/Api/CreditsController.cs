using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KeplerCMS.Controllers
{
    [Route("api/credits")]
    public class CreditsController : Controller
    {
        private readonly IUserService _userService;
        private readonly ICreditService _creditService;

        public CreditsController(IUserService userService, ICreditService creditService)
        {
            _userService = userService;
            _creditService = creditService;
        }

        [LoggedInFilter]
        [HttpPost("redeem")]
        public async Task<IActionResult> Redeem(string code)
        {
            var user = await _userService.GetUserById(int.Parse(User.Identity.Name));
            ViewData["success"] = await _creditService.RedeemCode(code, user.Id);
            return View();
        }
    }
}
