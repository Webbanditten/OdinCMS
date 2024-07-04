using System;
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
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }
        
        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Index(int currentPage = 1, string a = null, string Message = null)
        {
            ViewBag.Message = Message;
            const int pageSize = 25;
            var searchResult = await _auditLogService.GetLogs(currentPage, 25, a, 0);
            var model = new AuditLogViewModel
            {
                AuditLogs = searchResult.AuditLogs,
                Action = a,
                CurrentPage = currentPage,
                TotalPages =  1 + (searchResult.TotalResults / pageSize),
                Actions = await _auditLogService.GetActions()
            };
            return View(model);
        }
    }
}
