using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KeplerCMS.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace KeplerCMS.Services.Implementations
{
    /// <summary>
    /// The hotel's own game data as the Shockwave client reads it: external_texts (ISO-8859-1) and external_variables
    /// from keplercms:texts / keplercms:vars, cached for ten minutes. Lets housekeeping show texts and image URLs exactly
    /// as the client does (e.g. the catalogue preview).
    /// </summary>
    public class GameDataService : IGameDataService
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(10);
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        public GameDataService(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
        }

        private async Task<Dictionary<string, string>> Load(string configKey)
        {
            var url = _configuration.GetSection("keplercms")[configKey];
            if (string.IsNullOrEmpty(url)) return new Dictionary<string, string>();

            return await _cache.GetOrCreateAsync("gamedata:" + configKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheFor;
                var values = new Dictionary<string, string>();
                try
                {
                    var bytes = await Http.GetByteArrayAsync(url);
                    // The texts are ISO-8859-1, like the client expects them.
                    foreach (var line in Encoding.Latin1.GetString(bytes).Split('\n'))
                    {
                        var index = line.IndexOf('=');
                        if (index <= 0) continue;
                        values[line[..index].Trim()] = line[(index + 1)..].TrimEnd('\r');
                    }
                }
                catch (Exception)
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                }
                return values;
            });
        }

        public async Task<Dictionary<string, string>> GetTexts(IEnumerable<string> keys)
        {
            var texts = await Load("texts");
            return keys.Distinct().Where(texts.ContainsKey).ToDictionary(k => k, k => texts[k]);
        }

        public async Task<string> GetVariable(string key, string fallback)
        {
            var vars = await Load("vars");
            return vars.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
        }

        /// <summary>image.library.url over https (the images host serves both; housekeeping pages are https).</summary>
        public async Task<string> GetImageLibraryUrl()
        {
            var url = await GetVariable("image.library.url", "https://images.habbo-danmark.com/c_images/");
            if (url.StartsWith("http://")) url = "https://" + url["http://".Length..];
            return url.EndsWith("/") ? url : url + "/";
        }

        /// <summary>The image names in the library folder that end in _&lt;language&gt; (the suffix removed).</summary>
        public async Task<List<string>> GetLibraryImages(string folder, string language)
        {
            var url = await GetImageLibraryUrl() + folder.Trim('/') + "/";
            return await _cache.GetOrCreateAsync("gamedata:library:" + url + language, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheFor;
                try
                {
                    var html = await Http.GetStringAsync(url);
                    var suffix = "_" + language + ".gif";
                    return Regex.Matches(html, "href=\"([^\"?/]+)\"")
                        .Select(m => Uri.UnescapeDataString(m.Groups[1].Value))
                        .Where(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                        .Select(n => n[..^suffix.Length])
                        .Distinct().OrderBy(n => n).ToList();
                }
                catch (Exception)
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
                    return new List<string>();
                }
            });
        }
    }
}
