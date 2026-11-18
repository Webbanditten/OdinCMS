using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KeplerCMS.Services.Implementations
{
    /// <summary>
    /// Ads (cms_ads): picking one for a slot, serving its image (and counting the view) and counting clicks. Billboards
    /// with fit "auto" are slanted here to each room's frame format (<see cref="BillboardFormats"/>).
    /// </summary>
    public class AdService : IAdService
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly AdCounters _counters;

        // Only these formats are decoded (the images are staff uploads; no TIFF/BMP/ICC-heavy decoders).
        private static readonly Configuration DecodeConfig = new(new PngConfigurationModule(), new GifConfigurationModule(),
            new JpegConfigurationModule(), new WebpConfigurationModule());

        public AdService(DataContext context, IWebHostEnvironment env, IMemoryCache cache, AdCounters counters)
        {
            _context = context;
            _env = env;
            _cache = cache;
            _counters = counters;
        }

        public async Task<List<Ad>> GetActive(string placement)
        {
            var now = DateTime.Now;
            var ads = await _context.Ads.AsNoTracking().Where(a => a.Placement == placement && a.Enabled).ToListAsync();
            return ads.Where(a => a.IsActive(now)).ToList();
        }

        public Ad PickWeighted(IReadOnlyList<Ad> ads)
        {
            if (ads.Count == 0) return null;
            var total = ads.Sum(a => Math.Max(1, a.Weight));
            var roll = Random.Shared.Next(total);
            foreach (var ad in ads)
            {
                roll -= Math.Max(1, ad.Weight);
                if (roll < 0) return ad;
            }
            return ads[^1];
        }

        public async Task<string> RoomFormat(int roomId)
        {
            var frame = await _context.BillboardFrames.AsNoTracking().FirstOrDefaultAsync(f => f.RoomId == roomId);
            return frame?.Format;
        }

        public async Task<(byte[] Bytes, string ContentType)?> LoadImage(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            path = path.Split('?')[0];
            if (!path.StartsWith("/")) path = "/" + path;
            if (path.Contains("..")) return null;

            // A site file (wwwroot: /c_images/..., /images/...).
            var file = _env.WebRootFileProvider.GetFileInfo(path);
            if (file.Exists && !file.IsDirectory)
            {
                await using var stream = file.CreateReadStream();
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);
                return (memory.ToArray(), ContentTypeOf(path));
            }

            // A media library upload: /images/<category>/<name>.
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3 && parts[0] == "images")
            {
                var upload = await _context.Uploads.AsNoTracking()
                    .Where(u => u.Category == parts[1] && u.Name == parts[2]).FirstOrDefaultAsync();
                if (upload != null) return (upload.Blob, upload.ContentType ?? ContentTypeOf(path));
            }
            return null;
        }

        public async Task<(byte[] Bytes, string ContentType)?> RenderBillboard(Ad ad, string format, int dx, int dy)
        {
            dx = Math.Clamp(dx, -1, 1);
            dy = Math.Clamp(dy, -1, 1);
            var auto = ad.Fit == BillboardFormats.Auto;
            var source = await LoadImage(ad.Image);
            if (source == null) return null;
            if (!auto && dx == 0 && dy == 0) return source;

            var key = $"billboard:{ad.Id}:{ad.UpdatedAt.Ticks}:{ad.Image}:{(auto ? format : ad.Fit)}:{dx}:{dy}:{ad.CropX}:{ad.CropY}:{ad.CropZoom}";
            if (_cache.TryGetValue(key, out byte[] cached)) return (cached, "image/png");
            var png = auto ? Slant(source.Value.Bytes, BillboardFormats.Get(format), dx, dy, ad.CropX, ad.CropY, ad.CropZoom) : Shift(source.Value.Bytes, dx, dy);
            if (png == null) return null;
            _cache.Set(key, png, TimeSpan.FromHours(12));
            return (png, "image/png");
        }

        /// <summary>
        /// A flat picture as a billboard: scaled to cover the frame's face (160×60 or 200×80), sheared 1:2 like the frames'
        /// screens (rising to the right in left-facing frames), on white with a 1 px dark outline. The client draws it with
        /// matte ink, which makes the white around the parallelogram transparent; the outline keeps white inside the
        /// picture from touching it. dx/dy move it for frames whose centre is 1 px off (see <see cref="Canvas"/>).
        /// cropX/cropY/zoom pick the part of the picture (<see cref="CropFor"/>); wwwroot/housekeeping/js/ads.js draws the
        /// same in the form's preview.
        /// </summary>
        public static byte[] Slant(byte[] flatBytes, BillboardFormats.Format format, int dx = 0, int dy = 0, double cropX = 0.5, double cropY = 0.5, double zoom = 1)
        {
            try
            {
                using var flat = Image.Load<Rgba32>(new DecoderOptions { Configuration = DecodeConfig, MaxFrames = 1 }, flatBytes);
                var w = format.Width;
                var half = w / 2;
                var faceH = format.FaceHeight;
                var crop = CropFor(flat.Width, flat.Height, w, faceH, cropX, cropY, zoom);
                flat.Mutate(x => x.Crop(crop).Resize(new ResizeOptions { Size = new Size(w, faceH), Mode = ResizeMode.Stretch, Sampler = KnownResamplers.Bicubic }));

                var (output, ox, oy) = Canvas(w, format.Height, dx, dy);
                using (output)
                {
                    var outline = new Rgba32(30, 30, 30, 255);
                    for (var x = 0; x < w; x++)
                    {
                        // The frames' screens step on odd columns: left-facing 80,79,79,78,... from the left edge.
                        var top = format.Left ? half - (x + 1) / 2 : (x + 1) / 2;
                        for (var v = 0; v < faceH; v++)
                        {
                            var px = flat[x, v];
                            // Flatten transparency onto white; keep pure white off the matte colour.
                            var a = px.A / 255f;
                            var r = (byte)Math.Round(px.R * a + 255 * (1 - a));
                            var g = (byte)Math.Round(px.G * a + 255 * (1 - a));
                            var b = (byte)Math.Round(px.B * a + 255 * (1 - a));
                            if (r == 255 && g == 255 && b == 255) b = 254;
                            var edge = v == 0 || v == faceH - 1 || x == 0 || x == w - 1;
                            output[ox + x, oy + top + v] = edge ? outline : new Rgba32(r, g, b, 255);
                        }
                    }
                    return Png(output);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// The part of a picture a billboard screen of faceW x faceH shows: the screen's shape, scaled to just cover the picture
        /// and then zoomed in (1..4), centred on (cropX, cropY) (0..1 of the picture's width and height) and kept inside the
        /// picture. 0.5, 0.5, 1 is the middle, as before the crop existed.
        /// </summary>
        public static Rectangle CropFor(int srcW, int srcH, int faceW, int faceH, double cropX, double cropY, double zoom)
        {
            zoom = Math.Clamp(double.IsFinite(zoom) ? zoom : 1, 1, 4);
            var scale = Math.Max((double)faceW / srcW, (double)faceH / srcH) * zoom;
            var cw = Math.Clamp((int)Math.Round(faceW / scale), 1, srcW);
            var ch = Math.Clamp((int)Math.Round(faceH / scale), 1, srcH);
            var cx = (int)Math.Round(Math.Clamp((double.IsFinite(cropX) ? cropX : 0.5) * srcW - cw / 2.0, 0, srcW - cw));
            var cy = (int)Math.Round(Math.Clamp((double.IsFinite(cropY) ? cropY : 0.5) * srcH - ch / 2.0, 0, srcH - ch));
            return new Rectangle(cx, cy, cw, ch);
        }

        /// <summary>A ready-made billboard moved by dx/dy (white padding, which matte ink makes transparent).</summary>
        public static byte[] Shift(byte[] bytes, int dx, int dy)
        {
            try
            {
                using var source = Image.Load<Rgba32>(new DecoderOptions { Configuration = DecodeConfig, MaxFrames = 1 }, bytes);
                var (output, ox, oy) = Canvas(source.Width, source.Height, dx, dy);
                using (output)
                {
                    output.Mutate(c => c.DrawImage(source, new Point(ox, oy), 1f));
                    return Png(output);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// The client centres the picture on the frame (its registration point is the middle). To move the content by one
        /// pixel the picture grows by two on that axis, which moves its middle by one, and the content sits at 0 or 2.
        /// </summary>
        private static (Image<Rgba32> Image, int X, int Y) Canvas(int w, int h, int dx, int dy)
        {
            var image = new Image<Rgba32>(w + (dx != 0 ? 2 : 0), h + (dy != 0 ? 2 : 0), new Rgba32(255, 255, 255, 255));
            return (image, dx != 0 ? 1 + dx : 0, dy != 0 ? 1 + dy : 0);
        }

        private static byte[] Png(Image<Rgba32> image)
        {
            using var memory = new MemoryStream();
            image.SaveAsPng(memory);
            return memory.ToArray();
        }

        public void CountView(int adId) => _counters.View(adId);

        public void CountClick(int adId) => _counters.Click(adId);

        private static string ContentTypeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream",
        };
    }
}
