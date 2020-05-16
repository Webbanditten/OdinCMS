using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.MyHabbo.Models
{
    public enum DialogType
    {
        Inventory, WebStore
    }
    public class MainViewModel
    {
        public List<HomesCategories> Categories { get; set; }
        public List<CatalogItem> Items { get; set; }
        public List<InventoryItem> InventoryItems { get; set; }
        public DialogType Type { get; set; }
        public string InventoryType { get; set; }
    }
}
