using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeplerCMS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KeplerCMS.Controllers
{
    /// <summary>
    /// Game data the v31 catalogue (hh_cat_new) downloads, built from the database:
    /// furnidata (furnidata.load.url) maps the class ids Odin sends to class names, size and part colours;
    /// productdata (productdata.load.url) gives each offer (sale code) its name and description.
    /// Both are lines of Lingo lists, ISO-8859-1 like the external texts. No file extension, so Cloudflare doesn't
    /// cache them; the client adds a per-session parameter (%hash%) on top.
    /// </summary>
    public class GameDataController : Controller
    {
        public const string FurniDataCacheKey = "gamedata:furnidata";
        public const string ProductDataCacheKey = "gamedata:productdata";
        private static readonly Encoding Latin1 = Encoding.Latin1;
        private readonly DataContext _context;
        private readonly IMemoryCache _cache;

        public GameDataController(DataContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        /// <summary>A Lingo string literal; the client reads every value with value(), so quotes become apostrophes.</summary>
        private static string S(object value) =>
            "\"" + (value?.ToString() ?? "").Replace("\"", "'").Replace("\r", " ").Replace("\n", " ") + "\"";

        /// <summary>One list per line, 100 entries each (the client parses a few lines per frame).</summary>
        private static string Lines(IEnumerable<string> entries) =>
            string.Join("\n", entries.Chunk(100).Select(chunk => "[" + string.Join(",", chunk) + "]")) + "\n";

        private IActionResult Text(string body)
        {
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Cache-Control"] = "public, max-age=300";
            return File(Latin1.GetBytes(body), "text/plain; charset=iso-8859-1");
        }

        [HttpGet]
        [Route("gamedata/furnidata")]
        public async Task<IActionResult> FurniData()
        {
            var body = await _cache.GetOrCreateAsync(FurniDataCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                var definitions = await _context.ItemsDefinitions.AsNoTracking().OrderBy(d => d.Id).ToListAsync();
                // [type, class id (= definition id, what Odin sends), class, revision, default direction,
                //  x size, y size, part colours, name, description]
                return Lines(definitions.Select(d => "[" + string.Join(",",
                    S((d.Behavior ?? "").Contains("wall_item") ? "i" : "s"), S(d.Id), S(d.Sprite), S(0), S(0),
                    S(d.Length), S(d.Width), S(d.Colour), S(d.Name), S(d.Description)) + "]"));
            });
            return Text(body);
        }

        [HttpGet]
        [Route("gamedata/productdata")]
        public async Task<IActionResult> ProductData()
        {
            var body = await _cache.GetOrCreateAsync(ProductDataCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                var items = await _context.CatalogueItems.AsNoTracking().ToListAsync();
                var definitions = await _context.ItemsDefinitions.AsNoTracking().ToDictionaryAsync(d => d.Id);
                // [product code (= sale code, the offer name Odin sends), name, description, special text], named as
                // Odin names them: packages and special-sprite items use their own texts.
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var entries = new List<string>();
                foreach (var item in items.OrderBy(i => i.Order).ThenBy(i => i.Id))
                {
                    if (string.IsNullOrEmpty(item.SaleCode) || !seen.Add(item.SaleCode)) continue;
                    definitions.TryGetValue(item.DefinitionId, out var d);
                    var own = item.IsPackage || item.ItemSpecialSpriteId > 0;
                    var name = item.IsPackage ? item.PackageName : own ? item.Name : d?.Name;
                    var description = item.IsPackage ? item.PackageDescription : own ? item.Description : d?.Description;
                    entries.Add("[" + string.Join(",", S(item.SaleCode), S(name), S(description), S("")) + "]");
                }
                return Lines(entries);
            });
            return Text(body);
        }
    }
}
