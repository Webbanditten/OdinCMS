using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using KeplerCMS.Models;
using KeplerCMS.Data;

namespace KeplerCMS.Controllers
{
    public class ProfileController : Controller
    {
        private readonly DataContext _context;
        private readonly ILogger<HomeController> _logger;

        public ProfileController(ILogger<HomeController> logger, DataContext context)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if(User.Identity.IsAuthenticated)
            {
                var user = _context.Users.Where(u => u.Id == int.Parse(User.Identity.Name)).FirstOrDefault();
                ViewData["figure"] = user.Figure;
                ViewData["username"] = user.Username;

                return View();
            }

            return RedirectToAction("index", "home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
