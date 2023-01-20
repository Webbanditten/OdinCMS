using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly DataContext _context;

        public AuditLogService(DataContext context)
        {
            _context = context;
        }
        
        public async Task<AuditLog> AddLog(AuditLogType action, int userId, int targetId = 0)
        {
            return await AddLog(action, userId, null, null, targetId, 0);
        }

        public async Task<AuditLog> AddLog(AuditLogType action, int userId, string message, string extraNotes, int targetId = 0, int dataId = 0)
        {
            var log = new AuditLog
            {
                Action = action.ToString().ToLower(),
                UserId = userId,
                Message = message,
                ExtraNotes = extraNotes,
                TargetId = targetId,
                DataId = dataId,
                CreatedAt = DateTime.Now
            };
        
        
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<AuditLogSearchModel> GetLogs(int page, int pageSize, string action, int? userId)
        {
            var totalLogs = _context.AuditLogs.LongCount();
            List<AuditLog> logs = null;

            if (action != null)
            {
                logs = await _context.AuditLogs
                    .Where(x => x.Action == action && (userId != null || x.UserId == userId))
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            else
            {
                logs = await _context.AuditLogs
                    .Where(x => userId != null || x.UserId == userId)
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            
            foreach (var log in logs)
            {
                if(log.UserId != 0) {
                    log.User = _context.Users.FirstOrDefault(u => u.Id == log.UserId);
                }
                if(log.TargetId != 0) {
                    log.Target = _context.Users.FirstOrDefault(u => u.Id == log.TargetId);
                }
            }

            return new AuditLogSearchModel
            {
                TotalResults = totalLogs,
                AuditLogs = logs
            };
        }

        public async Task<IEnumerable<string>> GetActions()
        {
            return await _context.AuditLogs.Select(l => l.Action).Distinct().OrderBy(a => a).ToListAsync();
        }
    }
}
