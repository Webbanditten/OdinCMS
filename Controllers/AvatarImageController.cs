using System;
using System.Threading;
//habbo-imaging/avatarimage

using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using KeplerCMS.Avatara;
using KeplerCMS.Avatara.Figure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KeplerCMS.Controllers
{
    /// <summary>
    /// Habbo pictures. With keplercms:avatarRenderer:url set, a 25-digit figure (every user's) is drawn by the renderer
    /// (habbo-vibe-playground tools/furni-renderer, /avatar: the game's own Shockwave people casts, drawn like the 2006
    /// client draws a Habbo in a room). Anything else, the renderer unreachable or ?renderer=flash: Avatara (the
    /// Flash-era assets in wwwroot/images/habbo-imaging) as before.
    /// </summary>
    public class AvatarImageController : Controller
    {
        private static readonly System.Net.Http.HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
        // Loaded once, thread-safely. The old `if (figuredataReader == null)` check published the instance
        // before it was filled, so concurrent first requests (a page with several avatars right after a
        // restart) loaded into the same dictionaries at once and corrupted them (IndexOutOfRange in
        // Dictionary.Add), or rendered from a half-loaded reader.
        private static readonly Lazy<FiguredataReader> FigureData = new(() =>
        {
            var reader = new FiguredataReader();
            reader.LoadFigurePalettes();
            reader.loadFigureSetTypes();
            reader.LoadFigureSets();
            reader.LoadOldFigureData();
            FigureExtractor.GetParts();
            return reader;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        private static FiguredataReader figuredataReader => FigureData.Value;
        private IMemoryCache _cache;
        private IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AvatarImageController> _logger;

        public AvatarImageController(IMemoryCache cache, IUserService userService, IConfiguration configuration, ILogger<AvatarImageController> logger)
        {
            this._userService = userService;
            this._cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>The picture from the avatar renderer; null = Avatara draws it.</summary>
        private async Task<byte[]> FromCasts(string figure, string sex, bool ignoreCache)
        {
            var url = _configuration["keplercms:avatarRenderer:url"];
            if (string.IsNullOrEmpty(url) || Request.Query["renderer"] == "flash") return null;
            if (figure == null || !Regex.IsMatch(figure, @"^\d{25}$")) return null;
            // The renderer takes Avatara's parameter names; only the ones it reads go along (and make the cache key).
            var query = "figure=" + figure + (string.IsNullOrEmpty(sex) ? "" : "&sex=" + Uri.EscapeDataString(sex));
            foreach (var name in new[] { "size", "direction", "head_direction", "action", "gesture", "head", "frame", "crr", "drk" })
            {
                var value = Request.Query[name].ToString();
                if (value.Length > 0 && value.Length <= 20 && Regex.IsMatch(value, @"^[A-Za-z0-9,]+$"))
                    query += "&" + name + "=" + value;
            }
            var key = "avatar-casts:" + query;
            if (!ignoreCache && _cache.TryGetValue(key, out byte[] known)) return known;
            try
            {
                using var response = await Http.GetAsync(url.TrimEnd('/') + "/avatar?" + query);
                if (!response.IsSuccessStatusCode) return null;
                var bytes = await response.Content.ReadAsByteArrayAsync();
                _cache.Set(key, bytes, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) });
                return bytes;
            }
            catch (Exception e)
            {
                _logger.LogWarning("Avatar renderer unreachable for {Query}: {Message}", query, e.Message);
                return null;
            }
        }



        [HttpGet("habbo-imaging/avatarimage")]
        public async Task<IActionResult> AvatarImage()
        {
            

            string sex = null;
            int bodyDirection = 2;
            int headDirection = 2;
            string figure = null;
            string action = "std";
            string gesture = "sml";
            bool headOnly = false;
            int frame = 1;
            int carryDrink = -1;
            string size = "b";
            bool ignoreCache = false;

            if (Request.Query.ContainsKey("figure"))
            {
                Request.Query.TryGetValue("figure", out var value);
                figure = value.ToString();
            } else if(Request.Query.ContainsKey("habbo")) {
                Request.Query.TryGetValue("habbo", out var value);
                try {
                    var userObj = await _userService.GetUserByUsername(value);
                    if(userObj != null)
                    {
                        figure = userObj.Figure;
                        sex = userObj.Gender;
                    } else {
                        return NotFound();
                    }
                } catch {
                    return NotFound();
                }
                
            }

            if (Request.Query.ContainsKey("action"))
            {
                Request.Query.TryGetValue("action", out var value);
                action = value.ToString();
            }

            if (Request.Query.ContainsKey("gesture"))
            {
                Request.Query.TryGetValue("gesture", out var value);
                
                gesture = value.ToString();

                if(gesture.Length == 0)
                    gesture = "std";
            }

            if (Request.Query.ContainsKey("figure"))
            {
                Request.Query.TryGetValue("figure", out var value);
                figure = value.ToString();
            }

            if (Request.Query.ContainsKey("size"))
            {
                Request.Query.TryGetValue("size", out var value);
                size = value.ToString();
            }

            if (Request.Query.ContainsKey("head"))
            {
                Request.Query.TryGetValue("head", out var value);
                headOnly = value.ToString() == "1" || value.ToString() == "true";
            }

            if (Request.Query.ContainsKey("direction"))
            {
                Request.Query.TryGetValue("direction", out var value);

                if (int.TryParse(value.ToString(), out int n))
                {
                    bodyDirection = int.Parse(value.ToString());
                }
            }

            if (Request.Query.ContainsKey("head_direction"))
            {
                Request.Query.TryGetValue("head_direction", out var value);

                if (int.TryParse(value.ToString(), out int n))
                {
                    headDirection = int.Parse(value.ToString());
                }
            }

            if (Request.Query.ContainsKey("frame"))
            {
                Request.Query.TryGetValue("frame", out var value);

                if (int.TryParse(value.ToString(), out int n))
                {
                    int v = int.Parse(value.ToString());
                    frame = v < 1 ? 1 : v;
                }
            }

            if (Request.Query.ContainsKey("drk"))
            {
                Request.Query.TryGetValue("drk", out var value);
                action = (value.ToString() == "1" || value.ToString() == "true") ? "drk" : action;
            }

            if (Request.Query.ContainsKey("crr"))
            {
                Request.Query.TryGetValue("crr", out var value);

                if (int.TryParse(value.ToString(), out int n))
                {
                    carryDrink = int.Parse(value.ToString());
                }
            }

           if (Request.Query.ContainsKey("ignorecache"))
            {
                Request.Query.TryGetValue("ignorecache", out var value);
                ignoreCache = value.ToString() == "1" || value.ToString() == "true";
            }

            if (Request.Query.ContainsKey("sex"))
            {
                sex = Request.Query["sex"].ToString();
            }

            var fromCasts = await FromCasts(figure, sex, ignoreCache);
            if (fromCasts != null)
            {
                Response.Headers["Cache-Control"] = Request.Query.ContainsKey("habbo") ? "public, max-age=600" : "public, max-age=86400";
                return File(fromCasts, "image/png");
            }

            if (figure != null && figure.Length > 0)
            {
                var fig = new Avatar(figure, size, bodyDirection, headDirection, figuredataReader, action: action, gesture: gesture, headOnly: headOnly, frame: frame, carryDrink: carryDrink);
                
                string key = figure+size+bodyDirection+headDirection+action+gesture+headOnly+frame+carryDrink;
                byte[] file = null;
                _cache.TryGetValue<byte[]>(key, out file);
                if (file == null  || !(file.Length > 0) || ignoreCache)
                {
                    file = fig.Run();
                    _cache.Set<byte[]>(key, file);
                }
                
                return File(file, "image/png");
            }

            return null;
            /*
            var nameValues = HttpUtility.ParseQueryString(Request.QueryString.ToString());
            if(nameValues.Get("figure") != null)
            {
                var figure = nameValues.Get("figure");
                bool canConvertToIntegers = new Regex(@"^\d+").IsMatch(figure);
                if (canConvertToIntegers)
                {
                    figure = Helpers.FigureHelper.ConvertFigure(figure, nameValues.Get("direction") != null ? int.Parse(nameValues.Get("direction")) : 0);
                }
                nameValues.Set("figure", figure);
            }
            return Redirect("https://www.habbo.com/habbo-imaging/avatarimage?" + nameValues.ToString());
            */
        }


    }
}
