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
        private readonly IVoucherService _voucherService;

        public CreditsController(IUserService userService, IVoucherService voucherService)
        {
            _userService = userService;
            _voucherService = voucherService;
        }

        [LoggedInFilter]
        [HttpPost("redeem")]
        public async Task<IActionResult> Redeem(string code)
        {
            var user = await _userService.GetUserById(int.Parse(User.Identity.Name));
            return View(await _voucherService.Redeem(code, user.Id));
        }
    }
}
