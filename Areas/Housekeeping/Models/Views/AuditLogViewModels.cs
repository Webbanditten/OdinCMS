using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class AuditLogViewModel
    {
        public IEnumerable<AuditLog> AuditLogs { get; set; }
        public int CurrentPage { get; set; }
        public long TotalPages { get; set; }
        public string Action { get; set; }
        public IEnumerable<string> Actions { get; set; }

    }
    
    public class AuditLogSearchModel
    {
        public IEnumerable<AuditLog> AuditLogs { get; set; }
        public long TotalResults { get; set; }
    }
}
