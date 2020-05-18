using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.MyHabbo.Models
{
    public class HomeViewModel
    {
        public bool IsEditing { get; set; }
        public Homes Home { get; set; }
        public List<ItemViewModel> Items { get; set; }
        public Users HomeUser { get; set; }
    }
}
