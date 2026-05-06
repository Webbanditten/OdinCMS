using System;
using System.Threading.Tasks;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class CallsForHelpController : Controller
    {
        private readonly ICallsForHelpService _callsForHelpService;

        public CallsForHelpController(ICallsForHelpService callsForHelpService)
        {
            _callsForHelpService = callsForHelpService;
        }

        [HousekeepingFilter(Fuse.fuse_receive_calls_for_help)]
        public async Task<IActionResult> Index(string date)
        {
            var filterDate = string.IsNullOrEmpty(date) 
                ? DateTime.Today 
                : DateTime.Parse(date);

            var calls = await _callsForHelpService.GetByDate(filterDate);

            ViewBag.FilterDate = filterDate;
            return View(calls);
        }
    }
}
