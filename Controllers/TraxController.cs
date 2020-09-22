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
        private readonly ITraxService _traxService;
        public TraxController(IUserService userService, IHomeService homeService, ITraxService traxService)
        {
            _userService = userService;
            _homeService = homeService;
            _traxService = traxService;
        }

        [Route("trax/song/{traxId}")]
        public async Task<IActionResult> Index(int traxId)
        {
            var song = await _traxService.GetSingleSongById(traxId);
            var songOwnerDetails = await _userService.GetUserById(song.UserId);
            if (song == null) { Response.StatusCode = StatusCodes.Status404NotFound; return Content("SONG NOT FOUND"); }


            var track1 = _traxService.GetTrack(song.Data, 1);
            var track2 = _traxService.GetTrack(song.Data, 2);
            var track3= _traxService.GetTrack(song.Data, 3);
            var track4 = _traxService.GetTrack(song.Data, 4);
            var user = (songOwnerDetails != null) ? songOwnerDetails.Username : "Anonymous";
            return Content("status=0&name=" + song.Title + "&author=" + user + "&track1=" + track1 + "&track2=" + track2 + "&track3=" + track3 + "&track4=" + track4 + "");
            
        }

    }
}
