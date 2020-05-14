using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.MyHabbo.Models
{
    public class CatalogItem
    {
        public HomesCatalog Details { get; set; }
        public HomesItemData Definition { get; set; }   
    }
}
