using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class AuditLogController : Controller
    {
        private readonly ICommandQueueService _commandQueueService;
        private readonly IUserService _userService;

        public AuditLogController(ICommandQueueService commandQueueService, IUserService userService)
        {
            _commandQueueService = commandQueueService;
            _userService = userService;
        }
        
        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Index(string search = null, int currentPage = 1, string letter = null, string Message = null)
        {
            ViewBag.Message = Message;
            const int pageSize = 25;
            var take = pageSize;
            var skip = (currentPage - 1) * pageSize;
            var searchResult = await _userService.BanSearch(search, take, skip, letter);
            var model = new BansViewModel
            {
                Bans = searchResult.Bans,
                Search = search,
                Letter = letter,
                CurrentPage = currentPage,
                TotalPages = searchResult.TotalResults / pageSize
            };
            return View(model);
        }
    }
}
