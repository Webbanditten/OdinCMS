using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace KeplerCMS.Controllers
{
    public class TraxController : Controller
    {
        private readonly IUserService _userService;
        private readonly IHomeService _homeService;

        public TraxController(IUserService userService, IHomeService homeService)
        {
            _userService = userService;
            _homeService = homeService;
        }

        [Route("trax/song/{traxId}")]
        public async Task<IActionResult> Index(string username)
        {
            /*
             * 1:0,10;244,2;245,12;369,4;0,1;354,4;0,6;364,2;0,7:
             * 2:0,5;355,4;0,2;348,3;0,7;348,2;344,2;0,6;348,3;0,3;365,2;0,1;362,2;0,6:
             * 3:0,3;359,6;367,4;0,1;344,2;350,2;0,2;368,2;0,2;247,4;346,1;0,1;346,1;0,1;346,1;0,1;346,1;0,1;346,1;0,1;345,2;0,3;360,2;0,3:
             * 4:369,4;0,2;245,4;0,1;359,6;0,2;359,6;0,2;251,8;250,4;252,4;0,1;358,4: 
             
             */
            var track1 = "0,10;244,2;245,12;369,4;0,1;354,4;0,6;364,2;0,7";
            var track2 = "0,5;355,4;0,2;348,3;0,7;348,2;344,2;0,6;348,3;0,3;365,2;0,1;362,2;0,6";
            var track3 = "0,3;359,6;367,4;0,1;344,2;350,2;0,2;368,2;0,2;247,4;346,1;0,1;346,1;0,1;346,1;0,1;346,1;0,1;346,1;0,1;345,2;0,3;360,2;0,3";
            var track4 = "369,4;0,2;245,4;0,1;359,6;0,2;359,6;0,2;251,8;250,4;252,4;0,1;358,4";
            return Content("status=0&name=Test&author=Wizter&track1=" + track1 + "&track2=" + track2 + "&track3=" + track3 + "&track4=" + track4 + "");
            
        }

    }
}
