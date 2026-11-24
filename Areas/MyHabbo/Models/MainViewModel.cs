using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.MyHabbo.Models
{
    /// <summary>The store window for one item type (stickers or backgrounds): its categories and the first one's products.</summary>
    public class StoreViewModel
    {
        public string Type { get; set; }
        public List<HomesCategories> Categories { get; set; }
        public List<CatalogItem> Items { get; set; }
    }

    /// <summary>The inventory window for one item type (stickers, backgrounds or widgets).</summary>
    public class InventoryViewModel
    {
        public string Type { get; set; }
        public List<InventoryItem> Items { get; set; }
    }

    /// <summary>The purchase question for a product; Product is null when it can't be bought.</summary>
    public class PurchaseConfirmViewModel
    {
        public CatalogItem Product { get; set; }
        /// <summary>Shown above the question (the note editor's "no notes left").</summary>
        public string Notice { get; set; }
    }
}
