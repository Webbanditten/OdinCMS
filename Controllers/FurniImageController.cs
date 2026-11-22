using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using KeplerCMS.Chroma;
using System.Security.Cryptography;
using System.Text;
using KeplerCMS.Helpers;
using System.Linq;
using System.Text.RegularExpressions;
using KeplerCMS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KeplerCMS.Controllers
{
    /// <summary>
    /// Furni pictures (anim=1: animated, when the furni moves). With keplercms:furniRenderer:url set they come from the furni renderer (habbo-vibe-playground
    /// tools/furni-renderer: the game's own Shockwave furni casts, drawn like the client draws them, with the item
    /// definition's part colours). Whatever it can't draw, or with ?renderer=flash, comes from the Flash files
    /// (swfs/hof_furni, ChromaFurniture) as before.
    /// </summary>
    public class FurniImageController : Controller
    {
        private static readonly System.Net.Http.HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FurniImageController> _logger;

        public FurniImageController(DataContext context, IConfiguration configuration, IMemoryCache cache, ILogger<FurniImageController> logger)
        {
            _context = context;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>The definition a picture asks for ("chair_norja" + color 2 = chair_norja*2) and its part colours.</summary>
        private async Task<(string Sprite, string Colours)> Definition(string sprite, int? color)
        {
            var candidates = sprite.Contains('*') ? new[] { sprite } : color != null ? new[] { sprite + "*" + color, sprite } : new[] { sprite };
            var key = "furni-def:" + string.Join("|", candidates);
            if (_cache.TryGetValue(key, out (string, string) known)) return known;
            var found = (candidates.Last(), (string)null);
            foreach (var candidate in candidates)
            {
                var definition = await _context.ItemsDefinitions.AsNoTracking().Where(d => d.Sprite == candidate).OrderBy(d => d.Id)
                    .Select(d => new { d.Colour }).FirstOrDefaultAsync();
                if (definition != null)
                {
                    found = (candidate, definition.Colour);
                    break;
                }
            }
            _cache.Set(key, found, TimeSpan.FromMinutes(10));
            return found;
        }

        /// <summary>The picture from the furni renderer, cached on disk; null = use the Flash renderer.</summary>
        private async Task<IActionResult> FromCasts(string sprite, int? color, int? state, int direction, bool shadows, bool small, bool icon, bool anim)
        {
            var url = _configuration["keplercms:furniRenderer:url"];
            if (string.IsNullOrEmpty(url) || Request.Query["renderer"] == "flash") return null;
            if (sprite.Length > 100 || !Regex.IsMatch(sprite, @"^[A-Za-z0-9_.* -]+$")) return null;

            var (definition, colours) = Regex.IsMatch(sprite, @"^poster ?\d+$") ? (sprite, (string)null) : await Definition(sprite, color);
            // anim=1: an animated PNG if the furni moves; without a state the renderer picks the one it moves in (a lamp lit).
            var query = "sprite=" + Uri.EscapeDataString(definition) + "&direction=" + direction + (state != null ? "&state=" + state : "")
                + (string.IsNullOrEmpty(colours) ? "" : "&colors=" + Uri.EscapeDataString(colours))
                + (shadows ? "&shadow=1" : "") + (small ? "&small=1" : "") + (icon ? "&icon=1" : "") + (anim ? "&anim=1" : "");
            // format=gif: a GIF (animated with anim=1) for places that don't play animated PNGs; bg=RRGGBB flattens it.
            var gif = Request.Query["format"] == "gif";
            if (gif)
            {
                query += "&format=gif";
                var bg = Request.Query["bg"].ToString().TrimStart('#');
                if (Regex.IsMatch(bg, "^[0-9A-Fa-f]{6}$")) query += "&bg=" + bg;
            }
            // Kept a day, then drawn again, so renderer fixes reach every picture by themselves; while the renderer is
            // unreachable the old file stays in use.
            var path = "furni_export/_casts/" + Hash("casts1|" + query) + (gif ? ".gif" : ".png");
            var cached = System.IO.File.Exists(path);
            byte[] bytes = null;
            if (cached && System.IO.File.GetLastWriteTimeUtc(path) > DateTime.UtcNow.AddDays(-1))
            {
                bytes = await System.IO.File.ReadAllBytesAsync(path);
            }
            else
            {
                try
                {
                    using var response = await Http.GetAsync(url.TrimEnd('/') + "/furni?" + query);
                    if (response.IsSuccessStatusCode)
                    {
                        bytes = await response.Content.ReadAsByteArrayAsync();
                        Directory.CreateDirectory("furni_export/_casts");
                        await System.IO.File.WriteAllBytesAsync(path, bytes);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        if (cached) System.IO.File.Delete(path);
                        return null; // nothing to draw from the casts
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning("Furni renderer unreachable for {Query}: {Message}", query, e.Message);
                }
                if (bytes == null)
                {
                    if (!cached) return null;
                    bytes = await System.IO.File.ReadAllBytesAsync(path);
                }
            }
            Response.Headers["Cache-Control"] = "public, max-age=86400";
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            return File(bytes, gif ? "image/gif" : "image/png");
        }

        [Route("habbo-imaging/furni")]
        public async Task<IActionResult> Index(int traxId)
        {
            bool isSmallFurni = false;
            int renderState = 0;
            int renderDirection = 0;
            int color = 0;
            string sprite = null;
            bool renderBackground = false;
            bool renderShadows = false;
            bool cropImage = true;
            string renderCanvasColour = "transparent";
            bool renderIcon = false;


            if (Request.Query.ContainsKey("sprite"))
            {
                Request.Query.TryGetValue("sprite", out var value);
                sprite = value.ToString();
            }

            if (Request.Query.ContainsKey("s"))
            {   
                Request.Query.TryGetValue("s", out var value);

                if (value == "1" || value == "true")
                {
                    isSmallFurni = true;
                }
            }

            if (Request.Query.ContainsKey("small"))
            {
                Request.Query.TryGetValue("small", out var value);

                if (value == "1" || value == "true")
                {
                    isSmallFurni = true;
                }
            }

            if (Request.Query.ContainsKey("state"))
            {
                Request.Query.TryGetValue("state", out var value);

                if (value.ToString().IsNumeric())
                {
                    renderState = int.Parse(value.ToString());

                    if (renderState >= 101)
                    {
                        renderState = 0;
                    }
                }
            }

            if (Request.Query.ContainsKey("direction"))
            {
                Request.Query.TryGetValue("direction", out var value);

                if (value.ToString().IsNumeric())
                {
                    renderDirection = int.Parse(value.ToString());
                }
            }

            if (Request.Query.ContainsKey("rotation"))
            {
                Request.Query.TryGetValue("rotation", out var value);

                if (value.ToString().IsNumeric())
                {
                    renderDirection = int.Parse(value.ToString());
                }
            }

            if (Request.Query.ContainsKey("color"))
            {
                Request.Query.TryGetValue("color", out var value);

                if (value.ToString().IsNumeric())
                {
                    color = int.Parse(value.ToString());

                    if (color >= 16)
                    {
                        color = 0;
                    }
                }
            }

            if (Request.Query.ContainsKey("colour"))
            {
                Request.Query.TryGetValue("colour", out var value);

                if (StringHelper.IsNumeric(value.ToString()))
                {
                    color = int.Parse(value.ToString());

                    if (color >= 16)
                    {
                        color = 0;
                    }
                }
            }

            if (Request.Query.ContainsKey("bg"))
            {
                Request.Query.TryGetValue("bg", out var value);
                renderBackground = !(value.ToString().Equals("0") || value.ToString().Equals("false"));
            }

            if (Request.Query.ContainsKey("crop"))
            {
                Request.Query.TryGetValue("crop", out var value);
                cropImage = (value.ToString().Equals("1") || value.ToString().Equals("true"));
            }

            if (Request.Query.ContainsKey("shadow"))
            {
                Request.Query.TryGetValue("shadow", out var value);
                renderShadows = (value.ToString().Equals("1") || value.ToString().Equals("true"));
            }

            if (Request.Query.ContainsKey("canvas"))
            {
                Request.Query.TryGetValue("canvas", out var value);
                renderCanvasColour = value.ToString();
            }


            if (Request.Query.ContainsKey("icon"))
            {
                Request.Query.TryGetValue("icon", out var value);
                renderIcon = (value.ToString().Equals("1") || value.ToString().Equals("true"));
            }

            // The sprite names folders under furni_export: only plain sprite names ("../" made folders anywhere).
            if (sprite != null && (sprite.Length > 100 || !Regex.IsMatch(sprite, @"^[A-Za-z0-9_.* -]+$") || sprite.Contains("..")))
            {
                return NotFound();
            }

            if (sprite != null && sprite.Length > 0)
            {
                var hasColor = Request.Query.ContainsKey("color") || Request.Query.ContainsKey("colour");
                var anim = Request.Query["anim"] == "1" || Request.Query["anim"] == "true";
                var fromCasts = await FromCasts(sprite.Trim(), hasColor ? color : null, Request.Query.ContainsKey("state") ? renderState : null,
                    renderDirection, renderShadows, isSmallFurni, renderIcon, anim);
                if (fromCasts != null) return fromCasts;

                string fileNameUnique = string.Concat(sprite, isSmallFurni, renderState, renderDirection, color, renderShadows, renderBackground, renderCanvasColour, cropImage, renderIcon);
                string hashedUniqueName = Hash(fileNameUnique);

                if (!System.IO.Directory.Exists("furni_export/" + sprite + "/export"))
                {
                    Directory.CreateDirectory("furni_export/" + sprite + "/export");
                }

                if (!System.IO.File.Exists("furni_export/" + sprite + "/export/" + hashedUniqueName + ".png"))
                {
                    if (sprite != null && sprite.Length > 0)
                    {
                        if(!System.IO.File.Exists("swfs/hof_furni/" + sprite + ".swf")) return NotFound();


                        var furni = new ChromaFurniture("swfs/hof_furni/" + sprite + ".swf",
                                        isSmallFurni: isSmallFurni, renderState: renderState,
                                        renderDirection: renderDirection, colourId: color,
                                        renderShadows: renderShadows, renderBackground: renderBackground,
                                        renderCanvasColour: renderCanvasColour, cropImage: cropImage, renderIcon: renderIcon);
                        furni.Run();
                        var bytes = furni.CreateImage();

                        if (bytes != null)
                        {
                            System.IO.File.WriteAllBytes("furni_export/" + sprite + "/export/" + hashedUniqueName + ".png", bytes);
                        }
                        else
                        {
                            System.IO.File.WriteAllBytes("furni_export/" + sprite + "/export/" + hashedUniqueName + ".png", new byte[0]);

                        }

                    }
                }

                if (System.IO.File.Exists("furni_export/" + sprite + "/export/" + hashedUniqueName + ".png"))
                {
                    Response.Headers.Add("Cache-Control", "public, max-age=31536000");
                    // Public images; the housekeeping catalogue tints them in a canvas (part colours), also from other hosts.
                    Response.Headers["Access-Control-Allow-Origin"] = "*";
                    return File(System.IO.File.ReadAllBytes("furni_export/" + sprite + "/export/" + hashedUniqueName + ".png"), "image/png");
                }
            }

            return NotFound();
        }
        static string Hash(string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }

    }
}
