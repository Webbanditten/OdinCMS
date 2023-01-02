using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Models;
using KeplerCMS.Data.Models;
using System.Collections;
using System.Collections.Generic;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class PagesController : Controller
    {
        private readonly IPageService _pageService;
        public PagesController(IPageService pageService)
        {
            _pageService = pageService;
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> Index(string message = null)
        {
            ViewData["message"] = message;
            var pages = await _pageService.GetAll();
            return View(pages);
        }


        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public IActionResult Create()
        {
            return View();
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        [HttpPost]
        public async Task<IActionResult> Create(PageAddViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _pageService.AddDetails(model);
                return RedirectToAction("Index", "Pages", new { message = "Page created" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> Update(int id)
        {
            var details = await _pageService.GetPageById(id);
            return View(new PageUpdateViewModel { Id = details.Id, Hidden = details.Hidden, Name = details.Name, Slug = details.Slug, Design = details.Design, DisplayHeader = details.DisplayHeader, News = details.News, NewsHeader = details.NewsHeader});
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        [HttpPost]
        public async Task<IActionResult> Update(PageUpdateViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _pageService.UpdateDetails(model);
                return RedirectToAction("Index", "Pages", new { message = "Page edited" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> Manage(int id, string message = null)
        {
            ViewData["message"] = message;
            var page = await _pageService.GetPageObjById(id);
            return View(page);
        }




        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public IActionResult CreateContainer(int id, int column)
        {
            return View(new ContainerAddViewModel { PageId = id, Column = column });
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        [HttpPost]
        public async Task<IActionResult> CreateContainer(ContainerAddViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _pageService.AddContainer(model);
                return RedirectToAction("Manage", "Pages", new { id = model.PageId, message = "Container created" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> UpdateContainer(int id)
        {
            var details = await _pageService.GetContainerById(id);
            return View(new ContainerUpdateViewModel { Id = details.Id, Title = details.Title, Text = details.Text, Theme = details.Theme, Type = details.Type, PageId = details.PageId, Hidden = details.Hidden });
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        [HttpPost]
        public async Task<IActionResult> UpdateContainer(ContainerUpdateViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _pageService.UpdateContainer(model);
                return RedirectToAction("Manage", "Pages", new { id = model.PageId, message = "Container updated" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> Remove(int id)
        {
            await _pageService.Remove(id);
            return RedirectToAction("Index", "Pages", new { message = "Page Removed" });
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        public async Task<IActionResult> RemoveContainer(int id, int pageId)
        {
            await _pageService.RemoveContainer(id);
            return RedirectToAction("Manage", "Pages", new { id = pageId, message = "Container Removed" });
        }

        [HousekeepingFilter(Fuse.housekeeping_pages)]
        [HttpPost]
        public async Task<IActionResult> SaveGrid([FromBody] PageGrid model)
        {
            await _pageService.ArrangeContainers(model);
            return Content("OK");
        }
    }
}
