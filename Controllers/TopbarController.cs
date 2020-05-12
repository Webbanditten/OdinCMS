using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;

namespace KeplerCMS.Controllers
{
    public class TopbarController : Controller
    {
        [LoggedInFilter(false)]
        public IActionResult HabboClub()
        {
            return View();
        }

        [LoggedInFilter(false)]
        public IActionResult Credits()
        {
            return View();
        }
    }
}
