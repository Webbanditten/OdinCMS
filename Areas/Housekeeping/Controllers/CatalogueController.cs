using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// The catalogue editor: the in-game catalogue rebuilt from the client's own layouts (wwwroot/housekeeping/catalogue,
    /// rendered by catalogue-view.js) with pages, products and furni master data edited around it
    /// (wwwroot/housekeeping/js/catalogue-editor.js). Changes reach the hotel with Publish (Odin's reload_catalogue).
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.fuse_administrator_access)]
    public class CatalogueController : Controller
    {
        private readonly ICommandQueueService _commandQueueService;
        private readonly ICatalogueService _catalogue;
        private readonly IGameDataService _gameData;

        public CatalogueController(ICommandQueueService commandQueueService, ICatalogueService catalogue, IGameDataService gameData)
        {
            _commandQueueService = commandQueueService;
            _catalogue = catalogue;
            _gameData = gameData;
        }

        public IActionResult Index() => View();

        // ---- Game data for the preview ----

        /// <summary>
        /// What the preview needs from the hotel's game data: the texts it asks for (keys), the client's language and image
        /// library (c_images), the catalogue images there in that language, and the fuses pages use.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GameData(string keys)
        {
            var language = await _gameData.GetVariable("language", "en");
            // Where the client downloads a furni's graphics (dynamic.download.url + name template), over https.
            var downloadUrl = (await _gameData.GetVariable("dynamic.download.url", "")).Replace("http://", "https://");
            var nameTemplate = await _gameData.GetVariable("dynamic.download.name.template", "");
            var furniCastUrl = downloadUrl.Length > 0 && nameTemplate.Length > 0 ? downloadUrl.TrimEnd('/') + "/" + nameTemplate.TrimStart('/') : "";
            var pages = await _catalogue.GetPages();
            var fuses = new[] { "DEFAULT", "fuse_catalogue_clubshop", "fuse_catalogue_administrator" }
                .Concat(pages.Select(p => p.Fuse)).Distinct().ToList();
            return Json(new
            {
                texts = await _gameData.GetTexts((keys ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)),
                language,
                imageLibraryUrl = await _gameData.GetImageLibraryUrl(),
                libraryImages = await _gameData.GetLibraryImages("catalogue", language),
                fuses,
                furniCastUrl,
                behaviours = CatalogueFurniOptions.Behaviours,
                interactors = CatalogueFurniOptions.Interactors,
            });
        }

        // ---- Pages ----

        [HttpGet]
        public async Task<IActionResult> Pages() => Json(await _catalogue.GetPages());

        [HttpGet]
        public async Task<IActionResult> Page(int id)
        {
            var page = await _catalogue.GetPage(id);
            if (page == null) return NotFound("No such page.");
            var products = await _catalogue.GetProducts(id);
            return Json(new { page, products, duplicateSaleCodes = await _catalogue.DuplicateSaleCodes(products.Select(p => p.SaleCode)) });
        }

        [HttpPost]
        public async Task<IActionResult> SavePage([FromBody] CataloguePageEdit page)
        {
            if (page == null || string.IsNullOrWhiteSpace(page.Name)) return BadRequest("Give the page a name.");
            if (string.IsNullOrWhiteSpace(page.Layout) || !Regex.IsMatch(page.Layout, "^ctlg_[a-z0-9]+$")) return BadRequest("Pick a layout.");
            return Json(new { id = await _catalogue.SavePage(page) });
        }

        public class IdRequest
        {
            public int Id { get; set; }
            public int PageId { get; set; }
            public List<int> Ids { get; set; } = new();
        }

        [HttpPost]
        public async Task<IActionResult> DeletePage([FromBody] IdRequest request)
        {
            await _catalogue.DeletePage(request.Id);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ReorderPages([FromBody] IdRequest request)
        {
            await _catalogue.ReorderPages(request.Ids);
            return Ok();
        }

        // ---- Products ----

        [HttpPost]
        public async Task<IActionResult> SaveProduct([FromBody] CatalogueProductEdit product)
        {
            if (product == null || product.Id <= 0) return BadRequest("Missing product.");
            if (string.IsNullOrWhiteSpace(product.SaleCode)) return BadRequest("The sale code can't be empty: the client buys by it.");
            if (product.IsPackage && (product.Packages == null || product.Packages.Count == 0)) return BadRequest("A package needs at least one furni.");
            return Json(new { id = await _catalogue.SaveProduct(product) });
        }

        [HttpPost]
        public async Task<IActionResult> AddProducts([FromBody] IdRequest request)
        {
            if (request.PageId <= 0 || request.Ids.Count == 0) return BadRequest("Pick a page and furni.");
            return Json(new { ids = await _catalogue.AddProducts(request.PageId, request.Ids) });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveProduct([FromBody] IdRequest request)
        {
            await _catalogue.RemoveProduct(request.Id, request.PageId);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ReorderProducts([FromBody] IdRequest request)
        {
            await _catalogue.ReorderProducts(request.PageId, request.Ids);
            return Ok();
        }

        // ---- Furni master data ----

        [HttpGet]
        public async Task<IActionResult> Furni(string query) => Json(await _catalogue.SearchFurni(query));

        [HttpGet]
        public async Task<IActionResult> FurniItem(int id)
        {
            var furni = await _catalogue.GetFurni(id);
            return furni == null ? NotFound("No such furni.") : Json(furni);
        }

        public class SaveFurniRequest
        {
            public CatalogueFurni Furni { get; set; }
            /// <summary>Also put the furni on this page as a product.</summary>
            public int AddToPageId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SaveFurni([FromBody] SaveFurniRequest request)
        {
            var furni = request?.Furni;
            if (furni == null || string.IsNullOrWhiteSpace(furni.Sprite)) return BadRequest("The sprite (class name) is required: it names the furni's graphics.");
            if (!Regex.IsMatch(furni.Sprite.Trim(), @"^[A-Za-z0-9_\-*]+$")) return BadRequest("The sprite may only use letters, digits, _ - and * (colour variants: name*colour).");
            if (string.IsNullOrWhiteSpace(furni.Name)) return BadRequest("Give the furni a name.");
            if (await _catalogue.SpriteExists(furni.Sprite.Trim(), furni.Id)) return BadRequest("Another furni already uses the sprite \"" + furni.Sprite.Trim() + "\".");
            var id = await _catalogue.SaveFurni(furni);
            var productIds = request.AddToPageId > 0 ? await _catalogue.AddProducts(request.AddToPageId, new List<int> { id }) : new List<int>();
            return Json(new { id, productIds });
        }

        // ---- Publish ----

        /// <summary>Make the hotel reload the catalogue (pages, products, packages and furni definitions) from the database.</summary>
        [HttpPost]
        public IActionResult Publish()
        {
            _commandQueueService.QueueCommand(CommandQueueType.reload_catalogue, new CommandTemplate());
            return Ok();
        }
    }
}
