using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Westwind.Globalization;

namespace KeplerCMS.Areas.MyHabbo
{
    /// <summary>
    /// The Habbo Home's web store and inventory (js/myhabbo-store.js). The edit toolbar opens one item type at a time
    /// (stickers, widgets, backgrounds) in the inventory; "Køb ..." switches the same window to that type's store.
    /// Notes are bought and written in the note editor (NoteEditorController).
    /// </summary>
    [Area("MyHabbo")]
    public class StoreController : HomeEditController
    {
        private static readonly string[] StoreTypes = { "stickers", "backgrounds" };
        private static readonly string[] InventoryTypes = { "stickers", "backgrounds", "widgets" };

        private readonly ICreditService _creditService;

        public StoreController(IHomeService homeService, ICreditService creditService) : base(homeService)
        {
            _creditService = creditService;
        }

        /// <summary>The x-json header as JSON with non-ASCII escaped (header values must be ASCII; the script decodes it).</summary>
        private void SetJsonValue(object value) => SetJson(JsonSerializer.Serialize(value));

        private static object PreviewJson(CatalogItem product) => product.Definition.Type == "backgrounds"
            ? new { itemCount = product.Details.Amount, previewCssClass = product.Definition.CssClass + "_pre", titleKey = product.Definition.Name, bgCssClass = product.Definition.CssClass }
            : (object)new { itemCount = product.Details.Amount, previewCssClass = product.Definition.CssClass + "_pre", titleKey = product.Definition.Name };

        private static object[] InventoryJson(InventoryItem item) => new object[]
        {
            item.Definition.CssClass + "_pre", item.Definition.CssClass, item.Definition.Name, item.Definition.Type, null, item.Details.Amount,
        };

        private IActionResult Message(string key) => View("Message", DbRes.T(key, "habbohome"));

        // ------------------------------------------------------------------ inventory

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/store/inventory")]
        public async Task<IActionResult> Inventory(string type)
        {
            if (!InventoryTypes.Contains(type) || await EditableHome() is not var (homeId, userId)) return Error();

            var items = await HomeService.GetInventory(homeId, type, userId);
            // A placed widget's stack stays at 0: it lists greyed out and isn't the one shown first.
            var first = items.FirstOrDefault(i => i.Details.Amount > 0);
            if (first != null) SetJsonValue(InventoryJson(first));
            return View("Inventory", new InventoryViewModel { Type = type, Items = items });
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/store/inventory_items")]
        public async Task<IActionResult> InventoryItems(string type)
        {
            if (!InventoryTypes.Contains(type) || await EditableHome() is not var (homeId, userId)) return Error();

            var items = await HomeService.GetInventory(homeId, type, userId);
            return type == "widgets" ? View("InventoryItemsWidgets", items) : View(items);
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/store/inventory_preview")]
        public async Task<IActionResult> InventoryPreview(int itemId, string type)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();

            // Looked up in what this home's inventory lists, so a group's widgets preview on the group page too.
            var items = await HomeService.GetInventory(homeId, InventoryTypes.Contains(type) ? type : "stickers", userId);
            var item = items.FirstOrDefault(i => i.Details.Id == itemId && i.Details.Amount > 0);
            if (item != null) SetJsonValue(InventoryJson(item));
            return View();
        }

        // ------------------------------------------------------------------ store

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Main(string type)
        {
            if (!StoreTypes.Contains(type)) type = "stickers";

            var categories = await HomeService.GetStoreCategoryList(type);
            var first = categories.FirstOrDefault();
            var items = first == null ? new List<CatalogItem>() : await HomeService.GetStoreCatelog(first.Id, 0);
            if (items.Count > 0) SetJsonValue(new[] { PreviewJson(items[0]) });
            return View(new StoreViewModel { Type = type, Categories = categories, Items = items });
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Items(int categoryId)
        {
            return View(await HomeService.GetStoreCatelog(categoryId, 0));
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Preview(int productId)
        {
            var product = await HomeService.GetProduct(productId);
            if (product == null) return Error();

            SetJsonValue(new[] { PreviewJson(product) });
            return View(product);
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/store/background_warning")]
        public IActionResult BackgroundWarning()
        {
            return View();
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/store/purchase_confirm")]
        public async Task<IActionResult> PurchaseConfirm(int productId)
        {
            return View(new PurchaseConfirmViewModel { Product = await HomeService.GetProduct(productId) });
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/store/purchase_backgrounds")]
        [Route("myhabbo/store/purchase_stickers")]
        [Route("myhabbo/store/purchase_notes")]
        public async Task<IActionResult> PurchaseItem(int selectedId)
        {
            if (CurrentUserId is not int userId) return Error();

            var product = await HomeService.GetProduct(selectedId);
            if (product == null) return Message("store_not_purchasable");

            if (!await HomeService.CanReceiveItem(product.Details.ItemId, userId))
            {
                return Message("store_owned");
            }
            // Pay first: Purchase checks and takes the credits in one step.
            if (!await _creditService.Purchase(product.Details.Price, userId))
            {
                return Message("store_notenough_credits");
            }
            // Two clicks at once on a background: the second one is paid but can't be given, so it's paid back.
            if (!await HomeService.GiveItem(product.Details.ItemId, product.Details.Amount, userId))
            {
                await _creditService.Refund(product.Details.Price, userId);
                return Message("store_owned");
            }

            await _creditService.LogPurchase(userId, PurchaseLog.HomeStore, product.Definition.Name, product.Details.Price);
            return Content("OK");
        }
    }
}
