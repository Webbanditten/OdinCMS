
using System;
using System.Collections.Generic;
using System.Linq;
using KeplerCMS.Helpers;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping.Helpers
{
    public static class HousekeepingAccess { 
        public static bool HasAccess(IEnumerable<string> userFuses, IEnumerable<Fuse> fuses)
        {
            return fuses.Select(fuse => fuse.Description()).Any(fuse => userFuses.Contains(fuse.ToLower()));
        }
     }
}
