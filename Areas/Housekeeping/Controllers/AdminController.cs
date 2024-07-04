using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class AdminController : Controller
    {
        public AdminController()
        {
        }

        [HousekeepingFilter(new Fuse[] { Fuse.fuse_administrator_access, Fuse.housekeeping_ranks })]
        public IActionResult Index()
        {
            return View();
        }
    }
}
