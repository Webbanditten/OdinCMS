using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace KeplerCMS.Services.Implementations
{
    /// <summary>
    /// The picture of a Habbo Home sticker or background outside a Home page: the Home stylesheets
    /// (styles/myhabbo/assets.css, then css/assets.css) give every item's css_class a background image and, for stickers,
    /// its size. Read once per process.
    /// </summary>
    public static class HomeAssetImages
    {
        public record Picture(string Url, int Width, int Height);

        private static Dictionary<string, Picture> _pictures;
        private static readonly object Lock = new();
        private static readonly Regex Rule = new(@"div\.([A-Za-z0-9_\-]+)\s*\{([^}]*)\}", RegexOptions.Compiled);
        private static readonly Regex Image = new(@"background-image:\s*url\(\s*['""]?([^)'""]+)['""]?\s*\)", RegexOptions.Compiled);
        private static readonly Regex Width = new(@"(?<![-a-z])width:\s*(\d+)px", RegexOptions.Compiled);
        private static readonly Regex Height = new(@"(?<![-a-z])height:\s*(\d+)px", RegexOptions.Compiled);

        /// <summary>The picture for a css_class (a background's "_pre" preview first), or null.</summary>
        public static Picture Find(string webRoot, string cssClass)
        {
            if (string.IsNullOrEmpty(cssClass)) return null;
            var pictures = Load(webRoot);
            return pictures.GetValueOrDefault(cssClass + "_pre") ?? pictures.GetValueOrDefault(cssClass);
        }

        private static Dictionary<string, Picture> Load(string webRoot)
        {
            lock (Lock)
            {
                if (_pictures != null) return _pictures;
                var pictures = new Dictionary<string, Picture>(StringComparer.Ordinal);
                foreach (var file in new[] { "styles/myhabbo/assets.css", "css/assets.css" })
                {
                    var path = Path.Combine(webRoot, file);
                    if (!File.Exists(path)) continue;
                    foreach (Match rule in Rule.Matches(File.ReadAllText(path)))
                    {
                        var name = rule.Groups[1].Value;
                        var image = Image.Match(rule.Groups[2].Value);
                        if (!image.Success || pictures.ContainsKey(name)) continue;
                        var width = Width.Match(rule.Groups[2].Value);
                        var height = Height.Match(rule.Groups[2].Value);
                        pictures[name] = new Picture(image.Groups[1].Value,
                            width.Success ? int.Parse(width.Groups[1].Value) : 0,
                            height.Success ? int.Parse(height.Groups[1].Value) : 0);
                    }
                }
                return _pictures = pictures;
            }
        }
    }
}
