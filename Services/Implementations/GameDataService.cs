using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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

        /// <summary>
        /// The URL to fetch server-side. keplercms:gameDataInternal maps public origins to internal ones
        /// ("https://images.habbo-danmark.com=http://images-habbo"): on prod the CMS container can't reach the asset host
        /// through its public address. Browsers keep the public URLs.
        /// </summary>
        private string Internal(string url)
        {
            var map = _configuration.GetSection("keplercms")["gameDataInternal"];
            if (string.IsNullOrWhiteSpace(map) || string.IsNullOrEmpty(url)) return url;
            foreach (var pair in map.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length != 2) continue;
                var from = parts[0].Trim().TrimEnd('/');
                var to = parts[1].Trim().TrimEnd('/');
                foreach (var scheme in new[] { "https://", "http://" })
                {
                    var publicOrigin = scheme + from.Replace("https://", "").Replace("http://", "");
                    if (url.StartsWith(publicOrigin + "/", StringComparison.OrdinalIgnoreCase)) return to + url[publicOrigin.Length..];
                }
            }
            return url;
        }

        private async Task<Dictionary<string, string>> Load(string configKey)
        {
            var url = Internal(_configuration.GetSection("keplercms")[configKey]);
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

        /// <summary>
        /// Which of these images exist in the library folder in the given language (&lt;name&gt;_&lt;language&gt;.gif), checked
        /// with HEAD requests and cached. The folder has no live directory listing (c_images/catalogue/ is a mirrored
        /// page from 2009), so the names to check come from the caller.
        /// </summary>
        public async Task<List<string>> GetLibraryImages(string folder, string language, IEnumerable<string> names)
        {
            var baseUrl = await GetImageLibraryUrl() + folder.Trim('/') + "/";
            var found = new List<string>();
            var unknown = new List<string>();
            foreach (var name in names.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct())
            {
                if (_cache.TryGetValue("gamedata:image:" + baseUrl + name + "_" + language, out bool exists)) { if (exists) found.Add(name); }
                else unknown.Add(name);
            }
            using var limit = new System.Threading.SemaphoreSlim(8);
            var checks = unknown.Select(async name =>
            {
                await limit.WaitAsync();
                try
                {
                    var url = Internal(baseUrl + Uri.EscapeDataString(name) + "_" + language + ".gif");
                    using var request = new HttpRequestMessage(HttpMethod.Head, url);
                    using var response = await Http.SendAsync(request);
                    var exists = response.IsSuccessStatusCode;
                    _cache.Set("gamedata:image:" + baseUrl + name + "_" + language, exists, exists ? CacheFor : TimeSpan.FromMinutes(2));
                    return exists ? name : null;
                }
                catch (Exception)
                {
                    return null;
                }
                finally
                {
                    limit.Release();
                }
            });
            found.AddRange((await Task.WhenAll(checks)).Where(n => n != null));
            return found.OrderBy(n => n).ToList();
        }
    }
}
