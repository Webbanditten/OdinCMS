using Isopoh.Cryptography.Argon2;
using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
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
            var user = await _userService.GetUserById(User.Identity.Name);
            ViewData["success"] = await _creditService.RedeemCode(code, user.Id);
            return View();
        }
    }
}
