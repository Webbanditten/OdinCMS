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

namespace KeplerCMS.Controllers
{
    //[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AvatarImageController : Controller
    {
        private static FiguredataReader figuredataReader;
        private IMemoryCache _cache;

        public AvatarImageController(IMemoryCache cache)
        {
            this._cache = cache;
            if (figuredataReader == null)
            {
                figuredataReader = new FiguredataReader();
                figuredataReader.LoadFigurePalettes();
                figuredataReader.loadFigureSetTypes();
                figuredataReader.LoadFigureSets();
                figuredataReader.LoadOldFigureData();
                FigureExtractor.GetParts();
            }
        }



        [HttpGet("habbo-imaging/avatarimage")]
        public async Task<IActionResult> AvatarImage()
        {
            

            bool isSmall = false;
            int bodyDirection = 2;
            int headDirection = 2;
            string figure = null;
            string action = "std";
            string gesture = "sml";
            bool headOnly = false;
            int frame = 1;
            int carryDrink = -1;

            if (Request.Query.ContainsKey("figure"))
            {
                Request.Query.TryGetValue("figure", out var value);
                figure = value.ToString();
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
            }

            if (Request.Query.ContainsKey("figure"))
            {
                Request.Query.TryGetValue("figure", out var value);
                figure = value.ToString();
            }

            if (Request.Query.ContainsKey("size"))
            {
                Request.Query.TryGetValue("size", out var value);

                if (value == "s")
                {
                    isSmall = true;
                }
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

            if (figure != null && figure.Length > 0)
            {
                var fig = new Avatar(figure, isSmall, bodyDirection, headDirection, figuredataReader, action: action, gesture: gesture, headOnly: headOnly, frame: frame, carryDrink: carryDrink);
                
                string key = figure+isSmall+bodyDirection+headDirection+figuredataReader+action+gesture+headOnly+frame+carryDrink;
                byte[] file;
                if (!_cache.TryGetValue<byte[]>(key, out file))
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
