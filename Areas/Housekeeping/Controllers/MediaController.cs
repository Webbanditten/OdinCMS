using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// The media library behind the image pickers (wwwroot/housekeeping/js/media-library.js): uploads (cms_upload,
    /// served at /images/CATEGORY/NAME), promo pictures and room photos, plus uploading images from the picker (drop,
    /// paste, or pasted into an editor).
    /// </summary>
    [Area("Housekeeping")]
    public class MediaController : Controller
    {
        private const long MaxBytes = 5 * 1024 * 1024;
        private static readonly Dictionary<string, string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png", [".gif"] = "image/gif", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".webp"] = "image/webp",
        };
        private readonly DataContext _context;
        private readonly IAuditLogService _audit;
        private readonly IUserService _users;
        private readonly IWebHostEnvironment _environment;

        public MediaController(DataContext context, IAuditLogService audit, IUserService users, IWebHostEnvironment environment)
        {
            _context = context;
            _audit = audit;
            _users = users;
            _environment = environment;
        }

        // ---- The site's own images (wwwroot/images and wwwroot/c_images), read only ----

        /// <summary>Folders searched for site images; habbo-imaging holds thousands of avatar parts nobody picks.</summary>
        private static readonly string[] SiteRoots = { "images", "c_images" };
        private static readonly string[] SiteSkipped = { "images/habbo-imaging" };
        private static SortedDictionary<string, List<string>> _siteImages;
        private static readonly object SiteLock = new();

        /// <summary>Folder (relative to wwwroot, with /) → image file names. Read once; the files ship with the image.</summary>
        private SortedDictionary<string, List<string>> SiteImages()
        {
            lock (SiteLock)
            {
                if (_siteImages != null) return _siteImages;
                var index = new SortedDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                var root = _environment.WebRootPath;
                foreach (var top in SiteRoots)
                {
                    var dir = Path.Combine(root, top);
                    if (!Directory.Exists(dir)) continue;
                    foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        if (!ImageTypes.ContainsKey(Path.GetExtension(file))) continue;
                        var folder = Path.GetRelativePath(root, Path.GetDirectoryName(file)).Replace('\\', '/');
                        if (SiteSkipped.Any(s => folder.Equals(s, StringComparison.OrdinalIgnoreCase) || folder.StartsWith(s + "/", StringComparison.OrdinalIgnoreCase))) continue;
                        if (!index.TryGetValue(folder, out var names)) index[folder] = names = new List<string>();
                        names.Add(Path.GetFileName(file));
                    }
                }
                foreach (var names in index.Values) names.Sort(StringComparer.OrdinalIgnoreCase);
                return _siteImages = index;
            }
        }

        private static object SiteItem(string folder, string name) =>
            new { kind = "site", id = 0, category = "site:" + folder, name, url = "/" + folder + "/" + name, width = 0, height = 0 };

        /// <summary>The site image folders with how many images each holds.</summary>
        [HttpGet]
        [HousekeepingFilter(new[] { Fuse.housekeeping_upload, Fuse.housekeeping_pages, Fuse.housekeeping_news, Fuse.fuse_campaigns })]
        public IActionResult Folders() => Json(SiteImages().Select(f => new { path = f.Key, count = f.Value.Count }));

        [HttpGet]
        [HousekeepingFilter(new[] { Fuse.housekeeping_upload, Fuse.housekeeping_pages, Fuse.housekeeping_news, Fuse.fuse_campaigns })]
        public IActionResult Folder(string path)
        {
            path = (path ?? "").Trim('/');
            return SiteImages().TryGetValue(path, out var names) ? Json(names.Select(n => SiteItem(path, n))) : NotFound();
        }

        /// <summary>Site images whose path contains the query (at most 300).</summary>
        [HttpGet]
        [HousekeepingFilter(new[] { Fuse.housekeeping_upload, Fuse.housekeeping_pages, Fuse.housekeeping_news, Fuse.fuse_campaigns })]
        public IActionResult SearchSite(string query)
        {
            query = (query ?? "").Trim();
            if (query.Length < 2) return Json(Array.Empty<object>());
            return Json(SiteImages().SelectMany(f => f.Value.Select(n => (folder: f.Key, name: n)))
                .Where(x => (x.folder + "/" + x.name).Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(300).Select(x => SiteItem(x.folder, x.name)));
        }

        [HttpGet]
        [HousekeepingFilter(new[] { Fuse.housekeeping_upload, Fuse.housekeeping_pages, Fuse.housekeeping_news, Fuse.fuse_campaigns })]
        public async Task<IActionResult> List()
        {
            var uploads = await _context.Uploads.AsNoTracking()
                .Select(u => new { u.Id, u.Name, u.Category, u.ContentType }).ToListAsync();
            var pictures = await (from p in _context.PromoPictures.AsNoTracking()
                                  join r in _context.Rooms on p.RoomId equals r.Id into rooms
                                  from r in rooms.DefaultIfEmpty()
                                  select new { p.Id, p.UpdatedAt, Room = r.Name }).ToListAsync();
            var photos = await (from p in _context.RoomPhotos.AsNoTracking()
                                join r in _context.Rooms on p.RoomId equals r.Id into rooms
                                from r in rooms.DefaultIfEmpty()
                                select new { p.RoomId, p.TakenAt, p.Width, p.Height, Room = r.Name }).ToListAsync();

            var items = uploads
                .Where(u => (u.ContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase) || ImageTypes.ContainsKey(Path.GetExtension(u.Name ?? "")))
                .OrderByDescending(u => u.Id)
                .Select(u => new { kind = "upload", id = u.Id, category = (u.Category ?? "").ToLowerInvariant(), name = u.Name, url = $"/images/{(u.Category ?? "").ToLowerInvariant()}/{u.Name}", width = 0, height = 0 })
                .Concat(pictures.OrderByDescending(p => p.UpdatedAt).Select(p => new { kind = "promo-picture", id = p.Id, category = "promo pictures", name = (p.Room ?? "Room") + " (promo picture " + p.Id + ")", url = $"/promo-picture/{p.Id}.png?v={p.UpdatedAt.Ticks}", width = PromoPictureController.Width, height = PromoPictureController.Height }))
                .Concat(photos.OrderByDescending(p => p.TakenAt).Select(p => new { kind = "room-photo", id = p.RoomId, category = "room photos", name = (p.Room ?? "Room " + p.RoomId), url = $"/room-photo/{p.RoomId}.png?v={p.TakenAt.Ticks}", width = p.Width, height = p.Height }))
                .ToList();
            var categories = uploads.Select(u => (u.Category ?? "").ToLowerInvariant()).Where(c => c.Length > 0).Distinct().OrderBy(c => c).ToList();
            return Json(new { items, categories, canUpload = await CanUpload() });
        }

        /// <summary>Stores images in the category; a name taken in it gets a number (name-2.png).</summary>
        [HttpPost]
        [RequestSizeLimit(25 * 1024 * 1024)]
        [HousekeepingFilter(new[] { Fuse.housekeeping_upload, Fuse.housekeeping_pages, Fuse.housekeeping_news, Fuse.fuse_campaigns })]
        public async Task<IActionResult> Upload(List<IFormFile> files, string category)
        {
            if (!await CanUpload()) return StatusCode(403, new { error = "Uploading needs the housekeeping_upload right" });
            category = (category ?? "").Trim().ToLowerInvariant();
            if (!Regex.IsMatch(category, "^[a-z0-9_-]{1,32}$")) return BadRequest(new { error = "Pick a category (letters, digits, - and _)" });
            if (files == null || files.Count == 0) return BadRequest(new { error = "No files" });

            var taken = new HashSet<string>(await _context.Uploads.Where(u => u.Category.ToLower() == category).Select(u => u.Name.ToLower()).ToListAsync());
            var saved = new List<object>();
            var errors = new List<string>();
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
                if (!ImageTypes.TryGetValue(extension, out var contentType)) { errors.Add($"{file.FileName}: only png, gif, jpg and webp"); continue; }
                if (file.Length == 0 || file.Length > MaxBytes) { errors.Add($"{file.FileName}: empty or over 5 MB"); continue; }

                var stem = Regex.Replace(Path.GetFileNameWithoutExtension(file.FileName ?? "image"), "[^A-Za-z0-9_.-]+", "_").Trim('_', '.');
                if (stem.Length == 0) stem = "image";
                if (stem.Length > 80) stem = stem[..80];
                var name = stem + extension;
                for (var n = 2; taken.Contains(name.ToLowerInvariant()); n++) name = $"{stem}-{n}{extension}";
                taken.Add(name.ToLowerInvariant());

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                var upload = new Upload { Category = category, Name = name, ContentType = contentType, Blob = stream.ToArray() };
                _context.Uploads.Add(upload);
                await _context.SaveChangesAsync();
                saved.Add(new { kind = "upload", id = upload.Id, category, name, url = $"/images/{category}/{name}", width = 0, height = 0 });
            }
            if (saved.Count > 0) await _audit.AddLog(AuditLogType.file_upload, int.Parse(User.Identity.Name), saved.Count);
            return Json(new { items = saved, errors });
        }

        private async Task<bool> CanUpload()
        {
            var user = await _users.GetUserById(int.Parse(User.Identity.Name));
            return user?.Fuses?.Any(f => f.FuseName == nameof(Fuse.housekeeping_upload)) == true;
        }
    }
}
