using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.MyHabbo.Models
{
    public class BadgesViewModel
    {
       public ItemViewModel Item { get; set; }
       public int PageNumber { get; set; }
    }
}
