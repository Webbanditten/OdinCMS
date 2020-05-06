using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using System.Threading.Tasks;
using KeplerCMS.Services.Interfaces;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using System.Collections.Generic;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class MenuController : Controller
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        [HousekeepingFilter(Fuse.housekeeping_menu)]
        public async Task<IActionResult> Index(string message = null)
        {
            var menu = await _menuService.GetMenu();
            ViewData["menu"] = menu;
            ViewData["message"] = message;
            return View();
        }

        [HousekeepingFilter(Fuse.housekeeping_menu)]
        [HttpPost]
        public async Task<IActionResult> Index([FromBody] List<MenuReArrangeModel> model)
        {
            await _menuService.ReArrange(model);
            return Content("OK");
        }

        [HousekeepingFilter(Fuse.housekeeping_menu)]
        public IActionResult Create()
        {
            return View();
        }

        [HousekeepingFilter(Fuse.housekeeping_menu)]
        [HttpPost]
        public async Task<IActionResult> Create(MenuAddViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _menuService.Add(model);
                return RedirectToAction("Index", "Menu", new { message = "Menu created" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_menu)]
        public async Task<IActionResult> Update(int id)
        {
            var menu = await _menuService.Get(id);
            return View(new MenuUpdateViewModel{ Id = menu.Id, Href = menu.Href, Icon = menu.Icon, State = menu.State, Text = menu.Text });
        }

        [HousekeepingFilter(Fuse.housekeeping_menu)]
        [HttpPost]
        public async Task<IActionResult> Update(MenuUpdateViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _menuService.Update(model);
                return RedirectToAction("Index", "Menu", new { message = "Menu edited" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_menu)]
        public async Task<IActionResult> Remove(int id)
        {
            await _menuService.Remove(id);
            return RedirectToAction("Index", "Menu", new { message = "Menu Removed" });
        }
    }
}
