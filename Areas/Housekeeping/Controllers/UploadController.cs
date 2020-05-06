using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class UploadController : Controller
    {
        private readonly IUploadService _uploadService;

        public UploadController(IUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        [HousekeepingFilter(Fuse.housekeeping_upload)]
        public IActionResult Index()
        {
            return View(_uploadService.GetAll());
        }


        [HousekeepingFilter(Fuse.housekeeping_upload)]
        public IActionResult Create()
        {
            return View();
        }

        [HousekeepingFilter(Fuse.housekeeping_upload)]
        [HttpPost]
        public async Task<IActionResult> Create(UploadAddViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _uploadService.Add(model);
                return RedirectToAction("Index", "Upload", new { message = "File uploaded" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_upload)]
        public async Task<IActionResult> Update(int id)
        {
            var upload = await _uploadService.GetWithoutBlob(id);
            return View(upload);
        }

        [HousekeepingFilter(Fuse.housekeeping_upload)]
        [HttpPost]
        public async Task<IActionResult> Update(UploadUpdateViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _uploadService.Update(model);
                return RedirectToAction("Index", "Upload", new { message = "File edited" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.housekeeping_upload)]
        public async Task<IActionResult> Remove(int id)
        {
            await _uploadService.Remove(id);
            return RedirectToAction("Index", "Upload", new { message = "File Removed" });
        }
    }
}
