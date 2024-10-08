﻿using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [ResponseCache(Duration = 0)]
        public async Task<int> Credits()
        {
            var user = await _userService.GetUserById(int.Parse(User.Identity.Name));
            return user.Credits;
        }
    }
}
