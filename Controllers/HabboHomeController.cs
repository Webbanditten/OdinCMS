using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;

namespace KeplerCMS.Controllers
{
    [MenuFilter]
    public class HabboHomeController : Controller
    {
        private readonly IUserService _userService;


        public HabboHomeController(IUserService userService)
        {
            _userService = userService;
        }

        [Route("home/{username:minlength(1)}")]
        [LoggedInFilter(false)]
        public IActionResult Index(string username)
        {
            
            return View();
            
        }


    }
}
