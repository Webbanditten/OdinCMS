using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using KeplerCMS.Models;
using KeplerCMS.Data;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Services;

namespace KeplerCMS.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly DataContext _context;
        private readonly CommandQueueService _commandQueueService;

        public ProfileController(DataContext context)
        {
            _context = context;
            _commandQueueService = new CommandQueueService(_context);
        }

        public IActionResult Index()
        {
         
            var user = _context.Users.Where(u => u.Id == int.Parse(User.Identity.Name)).FirstOrDefault();
            ViewData["figure"] = user.Figure;
            ViewData["username"] = user.Username;
            ViewData["gender"] = user.Gender;
            ViewData["club"] = user.HasHabboClub();

            return View();
            
        }
        [HttpPost]
        public IActionResult UpdateFigure(string newGender, string figureData)
        {
            if((newGender == "M" || newGender == "F") && figureData.Length == 25)
            {
                var userId = User.Identity.Name;
                var user = _context.Users.Where(u => u.Id == int.Parse(userId)).FirstOrDefault();
                user.Figure = FigureService.FixFigure(figureData);
                user.Gender = newGender;
                _context.SaveChanges();

                _commandQueueService.QueueCommand(Models.Enums.CommandQueueType.refresh_appearance, userId);
            }
            return RedirectToAction("index");
        }
    }
}
