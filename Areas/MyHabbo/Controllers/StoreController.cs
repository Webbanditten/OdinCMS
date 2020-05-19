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
			Response.Headers.Add("x-json", "[[\""+ DbRes.T("inventory", "habbohome") + "\",\"" + DbRes.T("webstore", "habbohome") + "\"],[{\"itemCount\":1,\"previewCssClass\":\"" + cssClassForFirstItem + "_pre\", \"titleKey\":\"\"}]]");
			return View(new MainViewModel { Categories = categories, Items = firstCategoryItems, Type = DialogType.WebStore, InventoryItems = new List<InventoryItem>() });
		}
		[HttpPost]
		[LoggedInFilter]
		public async Task<IActionResult> Inventory(string type)
		{
			var userId = int.Parse(User.Identity.Name);
			var categories = await _homeService.GetStoreCategories();
			var InventoryItems = await _homeService.GetInventory(type, userId);
			var cssClassForFirstItem = "";
			var firstItemName = "";
			if (InventoryItems.Count > 0)
			{
				cssClassForFirstItem = InventoryItems.FirstOrDefault().Definition.CssClass;
				firstItemName = InventoryItems.FirstOrDefault().Definition.Name;
			}
			Response.Headers.Add("x-json", "[[\"" + DbRes.T("inventory", "habbohome") + "\",\"" + DbRes.T("webstore", "habbohome") + "\"],[\"" + cssClassForFirstItem + "_pre\", \"" + cssClassForFirstItem + "\", \"" + firstItemName + "\", \"\", null,1]]");
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
					Response.Headers.Add("x-json", "[\"" + invItem.Definition.CssClass + "_pre\",null,null,\"WebCommodity\",null,1]");
				} else
				{
					Response.Headers.Add("x-json", "[\"" + invItem.Definition.CssClass + "_pre\",\"" + invItem.Definition.CssClass + "\", \"" + invItem.Definition.Name + "\", \"\", null,1]");
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
		[Route("myhabbo/store/purchase_stickie_notes")]
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
				Response.Headers.Add("x-json", "[{\"bgCssClass\":\"" + itemInCategory.Definition.CssClass + "\",\"itemCount\":1,\"previewCssClass\":\"" + itemInCategory.Definition.CssClass + "_pre\", \"titleKey\":\"" + itemInCategory.Definition.Name + "\"}]");
			}
			else {
				Response.Headers.Add("x-json", "[{\"itemCount\":1,\"previewCssClass\":\"" + itemInCategory.Definition.CssClass + "_pre\", \"titleKey\":\"" + itemInCategory.Definition.Name + "\"}]");
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
			var items = await _homeService.GetInventory(type, userId);
			if(type == "widgets")
			{
				return View("InventoryItemsWidgets", items);
			}
			return View(items);
		}
	}
	///////////////////// Inventory
	/// type: notes,widgets,backgrounds,stickers

	/*
	 RESPONSE

	<div style="position: relative;">
	<div id="webstore-categories-container">
	<h4>Categories:</h4>
	<div id="webstore-categories">
	<ul class="purchase-main-category">
		<li id="maincategory-1-stickers" class="selected-main-category webstore-selected-main">
			<div>Stickers</div>
			<ul class="purchase-subcategory-list" id="main-category-items-1">


					<li id="subcategory-1-470-stickers" class="subcategory-selected">
						<div>Advertisments</div>
					</li>



					<li id="subcategory-1-452-stickers" class="subcategory">
						<div>Alhambra</div>
					</li>



					<li id="subcategory-1-182-stickers" class="subcategory">
						<div>Alphabet Bling</div>
					</li>



					<li id="subcategory-1-480-stickers" class="subcategory">
						<div>Alphabet Diner Blue</div>
					</li>



					<li id="subcategory-1-483-stickers" class="subcategory">
						<div>Alphabet Diner Green</div>
					</li>



					<li id="subcategory-1-485-stickers" class="subcategory">
						<div>Alphabet Diner Red</div>
					</li>



					<li id="subcategory-1-394-stickers" class="subcategory">
						<div>Alphabet Plastic</div>
					</li>



					<li id="subcategory-1-486-stickers" class="subcategory">
						<div>Alphabet Wood</div>
					</li>



					<li id="subcategory-1-454-stickers" class="subcategory">
						<div>Apartment 732</div>
					</li>



					<li id="subcategory-1-453-stickers" class="subcategory">
						<div>Avatars</div>
					</li>



					<li id="subcategory-1-457-stickers" class="subcategory">
						<div>Banks</div>
					</li>



					<li id="subcategory-1-481-stickers" class="subcategory">
						<div>Batman Darknight</div>
					</li>



					<li id="subcategory-1-446-stickers" class="subcategory">
						<div>Battle Ball</div>
					</li>



					<li id="subcategory-1-461-stickers" class="subcategory">
						<div>Beach</div>
					</li>



					<li id="subcategory-1-138-stickers" class="subcategory">
						<div>Borders</div>
					</li>



					<li id="subcategory-1-451-stickers" class="subcategory">
						<div>Buttons</div>
					</li>



					<li id="subcategory-1-468-stickers" class="subcategory">
						<div>Celebration</div>
					</li>



					<li id="subcategory-1-466-stickers" class="subcategory">
						<div>China</div>
					</li>



					<li id="subcategory-1-493-stickers" class="subcategory">
						<div>Christmas 07</div>
					</li>



					<li id="subcategory-1-105-stickers" class="subcategory">
						<div>Christmas 09</div>
					</li>



					<li id="subcategory-1-459-stickers" class="subcategory">
						<div>Costume</div>
					</li>



					<li id="subcategory-1-171-stickers" class="subcategory">
						<div>Cute</div>
					</li>



					<li id="subcategory-1-444-stickers" class="subcategory">
						<div>Deals</div>
					</li>



					<li id="subcategory-1-482-stickers" class="subcategory">
						<div>Diner</div>
					</li>



					<li id="subcategory-1-462-stickers" class="subcategory">
						<div>Easter</div>
					</li>



					<li id="subcategory-1-106-stickers" class="subcategory">
						<div>Flags</div>
					</li>



					<li id="subcategory-1-456-stickers" class="subcategory">
						<div>Football</div>
					</li>



					<li id="subcategory-1-467-stickers" class="subcategory">
						<div>Habbowood</div>
					</li>



					<li id="subcategory-1-488-stickers" class="subcategory">
						<div>Halloween</div>
					</li>



					<li id="subcategory-1-469-stickers" class="subcategory">
						<div>Highlighter</div>
					</li>



					<li id="subcategory-1-491-stickers" class="subcategory">
						<div>Hockey</div>
					</li>



					<li id="subcategory-1-471-stickers" class="subcategory">
						<div>Inked</div>
					</li>



					<li id="subcategory-1-476-stickers" class="subcategory">
						<div>Japanese</div>
					</li>



					<li id="subcategory-1-475-stickers" class="subcategory">
						<div>Keep It Real (NOT!)</div>
					</li>



					<li id="subcategory-1-490-stickers" class="subcategory">
						<div>Money</div>
					</li>



					<li id="subcategory-1-449-stickers" class="subcategory">
						<div>OB</div>
					</li>



					<li id="subcategory-1-130-stickers" class="subcategory">
						<div>Others</div>
					</li>



					<li id="subcategory-1-465-stickers" class="subcategory">
						<div>Paper Mario</div>
					</li>



					<li id="subcategory-1-270-stickers" class="subcategory">
						<div>Pointers</div>
					</li>



					<li id="subcategory-1-472-stickers" class="subcategory">
						<div>Prices</div>
					</li>



					<li id="subcategory-1-107-stickers" class="subcategory">
						<div>Promos</div>
					</li>



					<li id="subcategory-1-492-stickers" class="subcategory">
						<div>Rock</div>
					</li>



					<li id="subcategory-1-109-stickers" class="subcategory">
						<div>Safe Internet Day</div>
					</li>



					<li id="subcategory-1-487-stickers" class="subcategory">
						<div>Sea</div>
					</li>



					<li id="subcategory-1-479-stickers" class="subcategory">
						<div>Sparkle</div>
					</li>



					<li id="subcategory-1-443-stickers" class="subcategory">
						<div>Special Effects</div>
					</li>



					<li id="subcategory-1-442-stickers" class="subcategory">
						<div>Spring</div>
					</li>



					<li id="subcategory-1-484-stickers" class="subcategory">
						<div>St Patrick</div>
					</li>



					<li id="subcategory-1-458-stickers" class="subcategory">
						<div>Summer</div>
					</li>



					<li id="subcategory-1-473-stickers" class="subcategory">
						<div>Tiki</div>
					</li>



					<li id="subcategory-1-102-stickers" class="subcategory">
						<div>Trax</div>
					</li>



					<li id="subcategory-1-489-stickers" class="subcategory">
						<div>Valentine</div>
					</li>



					<li id="subcategory-1-460-stickers" class="subcategory">
						<div>WWE</div>
					</li>



					<li id="subcategory-1-445-stickers" class="subcategory">
						<div>Winter</div>
					</li>




			</ul>
		</li>
		<li id="maincategory-4-backgrounds" class="main-category">
			<div>Backgrounds</div>
			<ul class="purchase-subcategory-list" id="main-category-items-4">


					<li id="subcategory-1-104-stickers" class="subcategory">
						<div>Backgrounds</div>
					</li>


			</ul>
		</li>
		<li id="maincategory-3-stickie_notes" class="main-category-no-subcategories">
			<div>Notes</div>
			<ul class="purchase-subcategory-list" id="main-category-items-3">

				<li id="subcategory-3-101-stickie_notes" class="subcategory">
					<div>29</div>
				</li>

			</ul>
		</li>
	</ul>

	</div>
	</div>

	<div id="webstore-content-container">
	<div id="webstore-items-container">
		<h4>Select an item by clicking it</h4>
		<div id="webstore-items"><ul id="webstore-item-list">
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	<li class="webstore-item-empty"></li>
	</ul></div>
	</div>
	<div id="webstore-preview-container">
		<div id="webstore-preview-default"></div>
		<div id="webstore-preview"></div>
	</div>
	</div>

	<div id="inventory-categories-container">
	<h4>Categories:</h4>
	<div id="inventory-categories">
	<ul class="purchase-main-category">
	<li id="inv-cat-stickers" class="selected-main-category-no-subcategories">
		<div>Stickers</div>
	</li>
	<li id="inv-cat-backgrounds" class="main-category-no-subcategories">
		<div>Backgrounds</div>
	</li>
	<li id="inv-cat-widgets" class="main-category-no-subcategories">
		<div>Widgets</div>
	</li>
	<li id="inv-cat-notes" class="main-category-no-subcategories">
		<div>Notes</div>
	</li>
	</ul>

	</div>
	</div>

	<div id="inventory-content-container">
	<div id="inventory-items-container">
		<h4>Select an item by clicking it</h4>
		<div id="inventory-items">



		<ul id="inventory-item-list">


	<li id="inventory-item-31276" title="">
		<div class="webstore-item-preview s_bonbon_duck_146x146_pre">
			<div class="webstore-item-mask">
			</div>
		</div>
	</li>

	<li id="inventory-item-31256" title="">
		<div class="webstore-item-preview s_paper_clip_1_pre">
			<div class="webstore-item-mask">
			</div>
		</div>
	</li>



	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>


	</ul>
		</div>
	</div>

	<div id="inventory-preview-container">
		<div id="inventory-preview-default"></div>
		<div id="inventory-preview">
		<h4>&nbsp;</h4>

	<div id="inventory-preview-box" ></div>


	<div id="inventory-preview-place" class="clearfix">
	<div class="clearfix">
		<a href="#" class="new-button" id="inventory-place"><b>Place</b><i></i></a>
	</div>
	</div>


	</div>
	</div>
	</div>

	<div id="webstore-close-container">
	<div class="clearfix"><a href="#" id="webstore-close" class="new-button"><b>Close</b><i></i></a></div>
	</div>
	</div>

	 */












	///////////////////// Inventory_items
	/// type: notes,widgets,backgrounds,stickers
	///itemId: 31276
	/*
	 * RESPONSE FOR BACKGROUNDS, STICKERS, NOTES
	*/
	/*

	 <ul id="inventory-item-list">


	<li id="inventory-item-31276" title="Bonbon Duck" >
		<div class="webstore-item-preview s_bonbon_duck_146x146_pre">
			<div class="webstore-item-mask">

			</div>
		</div>

	</li>

	<li id="inventory-item-31256" title="Paper Clip 1" >
		<div class="webstore-item-preview s_paper_clip_1_pre">
			<div class="webstore-item-mask">

			</div>
		</div>

	</li>




	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>



	</ul>	
	 */

	/*

	RESPONSE FOR WIDGET 






	<ul id="inventory-item-list">


	<li id="inventory-item-p-31254" title="Rooms Widget" class="webstore-widget-item webstore-widget-disabled">
		<div class="webstore-item-preview w_roomswidget_pre">
			<div class="webstore-item-mask">

			</div>
		</div>

			<div class="webstore-widget-description">
				<h3>Rooms Widget</h3>
				<p>Show your rooms in your page</p>
			</div>

	</li>

	<li id="inventory-item-p-31255" title="High scores widget" class="webstore-widget-item webstore-widget-disabled">
		<div class="webstore-item-preview w_highscoreswidget_pre">
			<div class="webstore-item-mask">

			</div>
		</div>

			<div class="webstore-widget-description">
				<h3>High scores widget</h3>
				<p>Display your high scores</p>
			</div>

	</li>

	<li id="inventory-item-p-31262" title="Guestbook widget" class="webstore-widget-item webstore-widget-disabled">
		<div class="webstore-item-preview w_guestbookwidget_pre">
			<div class="webstore-item-mask">

			</div>
		</div>

			<div class="webstore-widget-description">
				<h3>Guestbook widget</h3>
				<p>Guestbook</p>
			</div>

	</li>

	<li id="inventory-item-p-31263" title="My Badges" class="webstore-widget-item webstore-widget-disabled">
		<div class="webstore-item-preview w_badgeswidget_pre">
			<div class="webstore-item-mask">

			</div>
		</div>

			<div class="webstore-widget-description">
				<h3>My Badges</h3>
				<p>Show your badges on your page.</p>
			</div>

	</li>

	<li id="inventory-item-p-31264" title="My friends widget" class="webstore-widget-item ">
		<div class="webstore-item-preview w_friendswidget_pre">
			<div class="webstore-item-mask">

			</div>
		</div>

			<div class="webstore-widget-description">
				<h3>My friends widget</h3>
				<p>Displays all your friends</p>
			</div>

	</li>

	<li id="inventory-item-p-31265" title="My groups widget" class="webstore-widget-item ">
		<div class="webstore-item-preview w_groupswidget_pre">
			<div class="webstore-item-mask">

			</div>
		</div>

			<div class="webstore-widget-description">
				<h3>My groups widget</h3>
				<p>Displays all your groups</p>
			</div>

	</li>

	<li id="inventory-item-p-31266" title="Traxplayer" class="webstore-widget-item webstore-widget-disabled">
		<div class="webstore-item-preview w_traxplayerwidget_pre">
			<div class="webstore-item-mask">

			</div>
		</div>

			<div class="webstore-widget-description">
				<h3>Traxplayer</h3>
				<p>Play Trax on your homepage.</p>
			</div>

	</li>

	<li id="inventory-item-p-31267" title="Rating widget" class="webstore-widget-item ">
		<div class="webstore-item-preview w_ratingwidget_pre">
			<div class="webstore-item-mask">

			</div>
		</div>

			<div class="webstore-widget-description">
				<h3>Rating widget</h3>
				<p>Allows members to vote on your page. You cannot vote for yourself.</p>
			</div>

	</li>




	</ul>



	 */











	///////////////////// Inventory_preview
	/// itemId: 31274
	///type: backgrounds
	///iemId: 31264
	///type: widgets
	///privileged: true


	/* OUTPUT */
	/*

	 <h4>&nbsp;</h4>

	<div id="inventory-preview-box"></div>

	<div id="inventory-preview-place" class="clearfix">
	<div class="clearfix">
		<a href="#" class="new-button" id="inventory-place"><b>Place</b><i></i></a>
	</div>
	</div>

	 */












	///////////////////// Items
	/// categoryId, subCategoryId

	/*
			<ul id="webstore-item-list">


	<li id="webstore-item-2" title="Trax Sfx">
		<div class="webstore-item-preview s_trax_sfx_pre">
			<div class="webstore-item-mask">

			</div>
		</div>
	</li>

	<li id="webstore-item-3" title="Trax Disco">
		<div class="webstore-item-preview s_trax_disco_pre">
			<div class="webstore-item-mask">

			</div>
		</div>
	</li>

	<li id="webstore-item-4" title="Trax 8 bit">
		<div class="webstore-item-preview s_trax_8_bit_pre">
			<div class="webstore-item-mask">

			</div>
		</div>
	</li>

	<li id="webstore-item-5" title="Trax Electro">
		<div class="webstore-item-preview s_trax_electro_pre">
			<div class="webstore-item-mask">

			</div>
		</div>
	</li>

	<li id="webstore-item-6" title="Trax Reggae">
		<div class="webstore-item-preview s_trax_reggae_pre">
			<div class="webstore-item-mask">

			</div>
		</div>
	</li>

	<li id="webstore-item-7" title="Trax Ambient">
		<div class="webstore-item-preview s_trax_ambient_pre">
			<div class="webstore-item-mask">

			</div>
		</div>
	</li>

	<li id="webstore-item-8" title="Trax Bling">
		<div class="webstore-item-preview s_trax_bling_pre">
			<div class="webstore-item-mask">

			</div>
		</div>
	</li>

	<li id="webstore-item-9" title="Trax Heavy">
		<div class="webstore-item-preview s_trax_heavy_pre">
			<div class="webstore-item-mask">

			</div>
		</div>
	</li>

	<li id="webstore-item-10" title="Trax Latin">
		<div class="webstore-item-preview s_trax_latin_pre">
			<div class="webstore-item-mask">

			</div>
		</div>
	</li>

	<li id="webstore-item-11" title="Trax Rock">
		<div class="webstore-item-preview s_trax_rock_pre">
			<div class="webstore-item-mask">

			</div>
		</div>
	</li>



	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>

	<li class="webstore-item-empty"></li>



	</ul>

	 */







	///////////////////// Preview
	/// productId, subCategoryId

	/*
	 <h4 title=""></h4>

	<div id="webstore-preview-box"></div>

	<div id="webstore-preview-price">
	Price:<br /><b>
		2 credits
	</b>
	</div>

	<div id="webstore-preview-purse">	
	You have:<br /><b>731 credits</b><br />
	<a href="https://classichabbo.com/credits" target=_blank>Get Credits</a>
	</div>

	<div id="webstore-preview-purchase" class="clearfix">
		<div class="clearfix">
			<a href="#" class="new-button" id="webstore-purchase"><b>Purchase</b><i></i></a>
		</div>
	</div>

	<span id="webstore-preview-bg-text" style="display: none">Preview</span>		


		 */











	///////////////////// Purchase_Confirm
	/// productId, subCategoryId
	/// 
	/*
	 <div class="webstore-item-preview s_bonbon_duck_146x146_pre">
	<div class="webstore-item-mask">
		
	</div>
</div>


<p>
Are you sure you want to purchase this product?</p>

<p class="new-buttons">
<a href="#" class="new-button" id="webstore-confirm-cancel"><b>Cancel</b><i></i></a>
<a href="#" class="new-button" id="webstore-confirm-submit"><b>Continue</b><i></i></a>
</p>

<div class="clear"></div>

	 */



	///////////////////// Purchase_stickers
	///task: purchase, selectedId: 1137
	/// OK

	///////////////////// Purchase_backgrounds
	///task: purchase, selectedId: 1137
	/// OK
	/// 

	///////////////////// purchase_stickie_notes
	///task: purchase, selectedId: 1137
	/// OK

}
