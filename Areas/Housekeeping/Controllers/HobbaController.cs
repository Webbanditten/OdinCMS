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
    public class HobbaController : Controller
    {
        public HobbaController() {}

        [HousekeepingFilter(Fuse.fuse_kick)]
        public async Task<IActionResult> Index()
        {
            return View();
        }
    }
}
