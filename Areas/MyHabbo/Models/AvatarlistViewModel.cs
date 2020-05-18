using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.MyHabbo.Models
{
    public class AvatarlistViewModel
    {
        public string Search { get; set; }
        public int PageNumber { get; set; }
        public ItemViewModel Widget { get; set; }
    }
}
