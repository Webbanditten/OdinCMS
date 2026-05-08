using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;

namespace KeplerCMS.Services.Interfaces
{
    public interface IAuditLogService
    {
        public Task<AuditLog> AddLog(AuditLogType action, int userId, string message, string extraNotes, int targetId = 0, int dataId = 0);
        public Task<AuditLog> AddLog(AuditLogType action, int userId, int targetId);
        public Task<AuditLogSearchModel> GetLogs(int page, int pageSize, string action, int? userId);
        public Task<IEnumerable<string>> GetActions();
    }
}
