using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using KeplerCMS.Services;

namespace KeplerCMS.Controllers
{
    [MaintenanceFilter]
    public class ClientController : Controller
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly CatalogueChoice _catalogue;

        public ClientController(IUserService userService, IConfiguration configuration, CatalogueChoice catalogue)
        {
            _userService = userService;
            _configuration = configuration;
            _catalogue = catalogue;
        }
        [LoggedInFilter]
        public async Task<IActionResult> Index(int forwardId = 0, int roomId = 0)
        {
            var userWithSSO = await _userService.GenerateSSO(int.Parse(User.Identity.Name));
            ViewData["sso"] = userWithSSO.SSOTicket;
            ViewData["forwardId"] = forwardId;
            ViewData["roomId"] = roomId;
            // The catalogue the player picked: Habbo r31's swaps the catalogue casts through its own variables file.
            ViewData["catalogueVars"] = await _catalogue.VarsFor(userWithSSO);
            return View();
        }

        /// <summary>
        /// The browser client: the same Habbo DCR and sw* params, run by DirPlayer (WASM) instead of the
        /// Shockwave plugin, talking to Odin's WebSocket listener. Needs keplercms:webclient:polyfill and
        /// :gameSocket; without them it falls back to the Shockwave client page.
        /// </summary>
        [LoggedInFilter]
        public async Task<IActionResult> Web(int forwardId = 0, int roomId = 0)
        {
            var webClient = _configuration.GetSection("keplercms:webclient");
            if (string.IsNullOrEmpty(webClient["polyfill"]) || string.IsNullOrEmpty(webClient["gameSocket"]))
            {
                return RedirectToAction(nameof(Index), new { forwardId, roomId });
            }

            var host = Request.Host.Host;
            if (host != "localhost" && host != "127.0.0.1")
            {
                // The external variables use http:// asset URLs and DirPlayer loads them with fetch(), which
                // browsers block on an https page instead of upgrading. This makes the browser upgrade every
                // http subresource, fetch and ws:// request to https/wss.
                Response.Headers["Content-Security-Policy"] = "upgrade-insecure-requests";
            }

            var userWithSSO = await _userService.GenerateSSO(int.Parse(User.Identity.Name));
            ViewData["sso"] = userWithSSO.SSOTicket;
            ViewData["forwardId"] = forwardId;
            ViewData["roomId"] = roomId;
            // The catalogue the player picked: Habbo r31's swaps the catalogue casts through its own variables file.
            ViewData["catalogueVars"] = await _catalogue.VarsFor(userWithSSO);
            return View();
        }
    }
}
