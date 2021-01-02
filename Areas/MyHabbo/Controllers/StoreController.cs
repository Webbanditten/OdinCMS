using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using Westwind.Globalization;
using KeplerCMS.Areas.MyHabbo.Models;
using Google.Protobuf.WellKnownTypes;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Internal;
using System.Web;

namespace KeplerCMS.Areas.MyHabbo
{
	[Area("MyHabbo")]
	public class StoreController : Controller
	{
		private readonly IHomeService _homeService;
		private readonly ICreditService _creditService;

		public StoreController(IHomeService homeService, ICreditService creditService)
		{
			_creditService = creditService;
			_homeService = homeService;
		}
		[HttpPost]
		[LoggedInFilter]
		public async Task<IActionResult> Main()
		{

			var categories = await _homeService.GetStoreCategories();
			var firstCategoryItems = await _homeService.GetStoreCatelog(categories.OrderBy(s => s.Order).FirstOrDefault().Id, 0);
			var cssClassForFirstItem = "";
			if(firstCategoryItems.Count > 0)
			{
				cssClassForFirstItem = firstCategoryItems.FirstOrDefault().Definition.CssClass;
			}
			Response.Headers.Add("x-json", "[[\""+ DbRes.T("inventory", "habbohome") + "\",\"" + DbRes.T("webstore", "habbohome") + "\"],[{\"itemCount\":1,\"previewCssClass\":\"" + HttpUtility.UrlEncode(cssClassForFirstItem) + "_pre\", \"titleKey\":\"\"}]]");
			return View(new MainViewModel { Categories = categories, Items = firstCategoryItems, Type = DialogType.WebStore, InventoryItems = new List<InventoryItem>() });
		}
		[HttpPost]
		[LoggedInFilter]
		public async Task<IActionResult> Inventory(string type)
		{
			var userId = int.Parse(User.Identity.Name);
			var homeId = int.Parse(Request.Cookies["editid"]);
			var categories = await _homeService.GetStoreCategories();
			var InventoryItems = await _homeService.GetInventory(homeId, type, userId);
			var cssClassForFirstItem = "";
			var firstItemName = "";
			if (InventoryItems.Count > 0)
			{
				cssClassForFirstItem = InventoryItems.FirstOrDefault().Definition.CssClass;
				firstItemName = InventoryItems.FirstOrDefault().Definition.Name;
			}
			Response.Headers.Add("x-json", "[[\"" + DbRes.T("inventory", "habbohome") + "\",\"" + DbRes.T("webstore", "habbohome") + "\"],[\"" + HttpUtility.UrlEncode(cssClassForFirstItem) + "_pre\", \"" + HttpUtility.UrlEncode(cssClassForFirstItem) + "\", \"" + HttpUtility.UrlEncode(firstItemName) + "\", \"\", null,1]]");
			return View("Main", new MainViewModel { InventoryType = type, Items = new List<CatalogItem>(), Categories = categories, InventoryItems = InventoryItems, Type = DialogType.Inventory, }); ;
		}

		[HttpPost]
		[LoggedInFilter]
		[Route("myhabbo/store/inventory_preview")]
		public async Task<IActionResult> InventoryPreview(int itemId)
		{
			var userId = int.Parse(User.Identity.Name);
			var invItem = await _homeService.GetInventoryItem(itemId, userId);
			if(invItem != null)
			{
				if (invItem.Definition.Type == "notes")
				{
					Response.Headers.Add("x-json", "[\"" + HttpUtility.UrlEncode(invItem.Definition.CssClass) + "_pre\",null,null,\"WebCommodity\",null,1]");
				} else
				{
					Response.Headers.Add("x-json", "[\"" + HttpUtility.UrlEncode(invItem.Definition.CssClass) + "_pre\",\"" + HttpUtility.UrlEncode(invItem.Definition.CssClass) + "\", \"" + HttpUtility.UrlEncode(invItem.Definition.Name) + "\", \"\", null,1]");
				}
			}

			
			return View();
		}

		[HttpPost]
		[LoggedInFilter]
		[Route("myhabbo/store/purchase_confirm")]
		public async Task<IActionResult> PurchaseConfirm(int productId)
		{
			var product = await _homeService.GetProduct(productId);
			return View(product);
		}

		[HttpPost]
		[LoggedInFilter]
		[Route("myhabbo/store/purchase_backgrounds")]
		[Route("myhabbo/store/purchase_stickers")]
		[Route("myhabbo/store/purchase_notes")]
		public async Task<IActionResult> PurchaseItem(int selectedId)
		{
			var userId = int.Parse(User.Identity.Name);
			var product = await _homeService.GetProduct(selectedId);
			var canPurchase = await _creditService.CanPurchase(product.Details.Price, userId);
			if(canPurchase)
			{
				var giveItem = await _homeService.GiveItem(product.Details.ItemId, product.Details.Amount, userId); ;
				if (giveItem)
				{
					await _creditService.Purchase(product.Details.Price, userId);
					return Content("OK");
				}
				return Content(DbRes.T("store_owned", "habbohome"));
			}
			return Content(DbRes.T("store_notenough_credits", "habbohome"));
		}

		[HttpPost]
		[LoggedInFilter]
		public async Task<IActionResult> Preview(int productId)
		{
			var itemInCategory = await _homeService.GetProduct(productId);
			if (itemInCategory.Definition.Type == "backgrounds")
			{
				Response.Headers.Add("x-json", "[{\"bgCssClass\":\"" + HttpUtility.UrlEncode(itemInCategory.Definition.CssClass) + "\",\"itemCount\":1,\"previewCssClass\":\"" + HttpUtility.UrlEncode(itemInCategory.Definition.CssClass) + "_pre\", \"titleKey\":\"" + HttpUtility.UrlEncode(itemInCategory.Definition.Name) + "\"}]");
			}
			else {
				Response.Headers.Add("x-json", "[{\"itemCount\":1,\"previewCssClass\":\"" + HttpUtility.UrlEncode(itemInCategory.Definition.CssClass) + "_pre\", \"titleKey\":\"" + HttpUtility.UrlEncode(itemInCategory.Definition.Name) + "\"}]");
			}

			return View(itemInCategory);
		}

		[HttpPost]
		[LoggedInFilter]
		public async Task<IActionResult> Items(int categoryId, int subCategoryId)
		{
				var items = await _homeService.GetStoreCatelog(categoryId, subCategoryId);
			return View(items);
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
			var userId = int.Parse(User.Identity.Name);
			var homeId = int.Parse(Request.Cookies["editid"]);
			if(await _homeService.CanEditHome(homeId, userId))
			{
				var items = await _homeService.GetInventory(homeId, type, userId);
				if (type == "widgets")
				{
					return View("InventoryItemsWidgets", items);
				}

				return View(items);
			}
			return Content("ERROR");
		}
	}
}
