using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.MyHabbo.Models
{
    public class GuestbookEntry
    {
        public Users User { get; set; }
        public HomesGuestbook Entry { get; set; }
    }
}
