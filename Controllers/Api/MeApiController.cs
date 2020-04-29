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
    [Route("api/me")]
    [ApiController]
    [Authorize]
    public class MeApiController : ControllerBase
    {
        private readonly IUserService _userService;

        public MeApiController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("credits")]
        public async Task<int> Credits()
        {
            var user = await _userService.GetUserById(User.Identity.Name);
            return user.Credits;
        }
    }
}
