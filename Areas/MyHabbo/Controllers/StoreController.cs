using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Westwind.Globalization;

namespace KeplerCMS.Areas.MyHabbo
{
    [Area("MyHabbo")]
    public class StoreController : HomeEditController
    {
        private readonly ICreditService _creditService;

        public StoreController(IHomeService homeService, ICreditService creditService) : base(homeService)
        {
            _creditService = creditService;
        }

        private static string Encode(string value) => HttpUtility.UrlEncode(value ?? string.Empty);

        private string TabTitles => "[\"" + DbRes.T("inventory", "habbohome") + "\",\"" + DbRes.T("webstore", "habbohome") + "\"]";

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Main()
        {
            var categories = await HomeService.GetStoreCategories();
            var first = categories.OrderBy(s => s.Order).FirstOrDefault();
            var items = first == null ? new List<CatalogItem>() : await HomeService.GetStoreCatelog(first.Id, 0);
            var previewClass = items.FirstOrDefault()?.Definition.CssClass ?? "";

            SetJson("[" + TabTitles + ",[{\"itemCount\":1,\"previewCssClass\":\"" + Encode(previewClass) + "_pre\", \"titleKey\":\"\"}]]");
            return View(new MainViewModel { Categories = categories, Items = items, Type = DialogType.WebStore, InventoryItems = new List<InventoryItem>() });
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Inventory(string type)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();

            var categories = await HomeService.GetStoreCategories();
            var inventory = await HomeService.GetInventory(homeId, type, userId);
            var first = inventory.FirstOrDefault()?.Definition;

            SetJson("[" + TabTitles + ",[\"" + Encode(first?.CssClass) + "_pre\", \"" + Encode(first?.CssClass) + "\", \"" + Encode(first?.Name) + "\", \"\", null,1]]");
            return View("Main", new MainViewModel { InventoryType = type, Items = new List<CatalogItem>(), Categories = categories, InventoryItems = inventory, Type = DialogType.Inventory });
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/store/inventory_preview")]
        public async Task<IActionResult> InventoryPreview(int itemId)
        {
            if (CurrentUserId is not int userId) return Error();

            var item = await HomeService.GetInventoryItem(itemId, userId);
            if (item != null)
            {
                var cssClass = Encode(item.Definition.CssClass);
                SetJson(item.Definition.Type == "notes"
                    ? "[\"" + cssClass + "_pre\",null,null,\"WebCommodity\",null,1]"
                    : "[\"" + cssClass + "_pre\",\"" + cssClass + "\", \"" + Encode(item.Definition.Name) + "\", \"\", null,1]");
            }
            return View();
        }

        [HttpPost]
        [LoggedInFilter]
        [Route("myhabbo/store/purchase_confirm")]
        public async Task<IActionResult> PurchaseConfirm(int productId)
        {
            var product = await HomeService.GetProduct(productId);
            return product == null ? Error() : View(product);
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
            if (product == null) return Error();

            if (!await HomeService.CanReceiveItem(product.Details.ItemId, userId))
            {
                return Content(DbRes.T("store_owned", "habbohome"));
            }
            // Pay first: Purchase checks and takes the credits in one step.
            if (!await _creditService.Purchase(product.Details.Price, userId))
            {
                return Content(DbRes.T("store_notenough_credits", "habbohome"));
            }

            await HomeService.GiveItem(product.Details.ItemId, product.Details.Amount, userId);
            await _creditService.LogPurchase(userId, PurchaseLog.HomeStore, product.Definition.Name, product.Details.Price);
            return Content("OK");
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Preview(int productId)
        {
            var product = await HomeService.GetProduct(productId);
            if (product == null) return Error();

            var definition = product.Definition;
            var background = definition.Type == "backgrounds" ? "\"bgCssClass\":\"" + Encode(definition.CssClass) + "\"," : "";
            SetJson("[{" + background + "\"itemCount\":1,\"previewCssClass\":\"" + Encode(definition.CssClass) + "_pre\", \"titleKey\":\"" + Encode(definition.Name) + "\"}]");
            return View(product);
        }

        [HttpPost]
        [LoggedInFilter]
        public async Task<IActionResult> Items(int categoryId, int subCategoryId)
        {
            return View(await HomeService.GetStoreCatelog(categoryId, subCategoryId));
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
        [Route("myhabbo/store/inventory_items")]
        public async Task<IActionResult> InventoryItems(string type)
        {
            if (await EditableHome() is not var (homeId, userId)) return Error();

            var items = await HomeService.GetInventory(homeId, type, userId);
            return type == "widgets" ? View("InventoryItemsWidgets", items) : View(items);
        }
    }
}
