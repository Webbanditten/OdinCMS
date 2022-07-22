
using System;
using System.Collections.Generic;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping.Helpers
{
    public static class HousekeepingAccess { 
        public static bool HasAccess(List<string> userFuses, KeplerCMS.Models.Fuse fuse)
        {
           var _fuse = Enum.GetName(typeof(Fuse), fuse);
           return (userFuses.Contains(_fuse.ToLower()));
        }
     }
}
