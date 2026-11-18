using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Services.Implementations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace KeplerCMS.Controllers
{
    /// <summary>
    /// The 2006 Christmas e-card (wwwroot/flash/xmas06/ecard.swf, Sulake's, with jeanropke/Neptune's endpoints, labels in
    /// ecard_da.xml): pick a Habbo, a background, a headline and a message, and send it. The card talks Flash Remoting
    /// (Amf0) to /api/ecard/gateway: Ecard.saveCard stores it (cms_ecards) and answers with the card's code,
    /// Ecard.loadCard(code) answers with the card's XML. /xmas/ecard?card=CODE shows a card; /api/user/gethabboimage is the
    /// avatar the card draws. The card lives on the CMS page xmas/julekort ({xmas_ecard}); a logged-in sender's card is also
    /// e-mailed to the friend's address (Mailgun, not kept), and every sender gets the card's link to share.
    /// </summary>
    public class EcardController : Controller
    {
        private const int MaxPerHour = 20;
        private const int MaxMailsPerDay = 10;
        private readonly DataContext _context;
        private readonly ILogger<EcardController> _logger;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private readonly KeplerCMS.Services.Interfaces.IMailService _mail;

        public EcardController(DataContext context, ILogger<EcardController> logger, Microsoft.Extensions.Caching.Memory.IMemoryCache cache, KeplerCMS.Services.Interfaces.IMailService mail)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _mail = mail;
        }

        /// <summary>The card moved into the Christmas section: the CMS page xmas/julekort ({xmas_ecard}).</summary>
        [HttpGet("xmas/ecard")]
        public IActionResult Index(string card) =>
            Redirect("/xmas/julekort" + (string.IsNullOrWhiteSpace(card) ? "" : "?card=" + Uri.EscapeDataString(card)));

        /// <summary>The visitor's latest cards (the page refreshes its list after sending).</summary>
        [HttpGet("xmas/ecard/mine")]
        public async Task<IActionResult> Mine()
        {
            if (User.Identity?.IsAuthenticated != true) return Json(Array.Empty<object>());
            var userId = int.Parse(User.Identity.Name);
            var cards = await _context.ECards.AsNoTracking().Where(c => c.UserId == userId).OrderByDescending(c => c.Id).Take(10)
                .Select(c => new { code = c.Code, toName = c.ToName, headline = c.Headline, createdAt = c.CreatedAt, emailed = c.EmailedAt != null }).ToListAsync();
            return Json(cards);
        }

        /// <summary>
        /// The Habbo the card draws: that Habbo's avatar as a GIF (the 2006 endpoint served GIFs; the card shows it when more
        /// than 100 bytes arrive, else "this Habbo does not exist"), fetched from our own avatar imager and cached a while.
        /// </summary>
        [HttpGet("api/user/gethabboimage")]
        public async Task<IActionResult> HabboImage(string habboname)
        {
            if (string.IsNullOrWhiteSpace(habboname)) return NotFound();
            var name = await _context.Users.AsNoTracking().Where(u => u.Username == habboname.Trim()).Select(u => u.Username).FirstOrDefaultAsync();
            if (name == null) return NotFound();
            // The card leaves its Habbo out of the card it saves when the Habbo is changed (Ruffle runs its "Habbo
            // is ready" step once): Save puts the last one the sender looked up in.
            _cache.Set(LastHabboKey(), name, TimeSpan.FromHours(2));

            var key = "ecard-habbo:" + name.ToLowerInvariant();
            if (!_cache.TryGetValue(key, out byte[] gif))
            {
                var url = "http://127.0.0.1:" + HttpContext.Connection.LocalPort + "/habbo-imaging/avatarimage?habbo=" + Uri.EscapeDataString(name) + "&direction=2&head_direction=2&size=b";
                try
                {
                    var png = await Http.GetByteArrayAsync(url);
                    using var image = SixLabors.ImageSharp.Image.Load(png);
                    using var output = new MemoryStream();
                    image.Save(output, new SixLabors.ImageSharp.Formats.Gif.GifEncoder());
                    gif = output.ToArray();
                    _cache.Set(key, gif, TimeSpan.FromMinutes(10));
                }
                catch (Exception e)
                {
                    _logger.LogWarning("E-card: no avatar for {Name} ({Message})", name, e.Message);
                    return NotFound();
                }
            }
            return File(gif, "image/gif");
        }

        private static readonly System.Net.Http.HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

        private string LastHabboKey() => "ecard-last:" + (User.Identity?.IsAuthenticated == true ? "u" + User.Identity.Name : "ip" + HttpContext.Connection.RemoteIpAddress);

        [HttpPost("api/ecard/gateway")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Gateway()
        {
            byte[] raw;
            using (var buffer = new MemoryStream())
            {
                await Request.Body.CopyToAsync(buffer);
                if (buffer.Length > 256 * 1024) return BadRequest();
                raw = buffer.ToArray();
            }

            Amf0.Request request;
            try
            {
                request = Amf0.Read(raw);
            }
            catch (FormatException e)
            {
                _logger.LogWarning("E-card gateway: unreadable request ({Message})", e.Message);
                return BadRequest();
            }

            var body = request.Bodies.FirstOrDefault();
            if (body == null) return BadRequest();
            var args = Amf0.Arguments(body.Value);

            object result = body.Target switch
            {
                "Ecard.saveCard" => await Save(args),
                "Ecard.loadCard" => await Load(args),
                _ => null,
            };
            return File(Amf0.Result(body.Response, result), "application/x-amf");
        }

        private async Task<object> Save(List<object> args)
        {
            var xml = args.FirstOrDefault() as string;
            if (string.IsNullOrWhiteSpace(xml) || xml.Length > 20000) return null;

            int? userId = User.Identity?.IsAuthenticated == true ? int.Parse(User.Identity.Name) : null;
            var since = DateTime.Now.AddHours(-1);
            var recent = userId != null
                ? await _context.ECards.CountAsync(c => c.UserId == userId && c.CreatedAt > since)
                : await _context.ECards.CountAsync(c => c.UserId == null && c.CreatedAt > since);
            if (recent >= (userId != null ? MaxPerHour : MaxPerHour * 2)) return null;

            // The card's arguments: its XML, (null), "your e-mail" (relabelled "Dit navn" in ecard_da.xml), the friend's
            // name, the friend's e-mail, a notify flag, the site url and the language.
            var habbo = Element(xml, "habboname");
            if (string.IsNullOrWhiteSpace(habbo) || habbo == "0" || habbo == "undefined")
            {
                if (_cache.TryGetValue(LastHabboKey(), out string last))
                {
                    habbo = last;
                    xml = SetElement(xml, "habboname", last);
                }
            }
            var card = new ECard
            {
                Code = NewCode(),
                UserId = userId,
                HabboName = Limit(habbo == "0" ? null : habbo, 50),
                FromName = Limit(args.ElementAtOrDefault(2) as string, 100),
                ToName = Limit(args.ElementAtOrDefault(3) as string, 100),
                Headline = Limit(Element(xml, "headline"), 255),
                Message = Limit(Element(xml, "msg"), 2000),
                CardXml = xml,
                CreatedAt = DateTime.Now,
            };
            _context.ECards.Add(card);
            await _context.SaveChangesAsync();

            // Logged-in senders: the card goes to the friend's e-mail too (the address isn't kept), at most MaxMailsPerDay a day.
            var to = (args.ElementAtOrDefault(4) as string)?.Trim();
            if (userId != null && ValidEmail(to))
            {
                var day = DateTime.Now.AddDays(-1);
                var mailed = await _context.ECards.CountAsync(c => c.UserId == userId && c.EmailedAt != null && c.EmailedAt > day);
                var sender = await _context.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Username).FirstOrDefaultAsync();
                if (mailed < MaxMailsPerDay && sender != null)
                {
                    var link = $"https://{Request.Host}/xmas/julekort?card={card.Code}";
                    try
                    {
                        var result = await _mail.SendEcard(to, sender, card.ToName, card.Headline, link);
                        if (result.Successful)
                        {
                            card.EmailedAt = DateTime.Now;
                            await _context.SaveChangesAsync();
                        }
                        else _logger.LogWarning("E-card {Code}: e-mail not sent ({Errors})", card.Code, string.Join(", ", result.ErrorMessages));
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning("E-card {Code}: e-mail failed ({Message})", card.Code, e.Message);
                    }
                }
            }
            return card.Code;
        }

        private async Task<object> Load(List<object> args)
        {
            var code = (args.FirstOrDefault() as string ?? "").Trim().Trim('/');
            var card = await _context.ECards.AsNoTracking().FirstOrDefaultAsync(c => c.Code == code);
            // The card reads event.result.xml.
            return card == null ? null : new Dictionary<string, object> { ["xml"] = card.CardXml };
        }

        private static bool ValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || email.Length > 255) return false;
            try { return new System.Net.Mail.MailAddress(email).Address == email; }
            catch (FormatException) { return false; }
        }

        private static string Limit(string s, int length) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().Length > length ? s.Trim().Substring(0, length) : s.Trim();

        private static string NewCode()
        {
            const string alphabet = "abcdefghjkmnpqrstuvwxyz23456789";
            var bytes = RandomNumberGenerator.GetBytes(10);
            return new string(bytes.Select(b => alphabet[b % alphabet.Length]).ToArray());
        }

        /// <summary>The card's XML with an element's text replaced (the card's own format; unchanged if it can't be read).</summary>
        private static string SetElement(string xml, string name, string value)
        {
            try
            {
                var doc = new XmlDocument { XmlResolver = null };
                doc.LoadXml(xml);
                var node = doc.SelectSingleNode("//" + name);
                if (node == null) return xml;
                node.InnerText = value;
                return doc.DocumentElement.OuterXml;
            }
            catch (XmlException)
            {
                return xml;
            }
        }

        /// <summary>The text of an element of the card's XML (habboname, headline, msg), or null.</summary>
        private static string Element(string xml, string name)
        {
            try
            {
                using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == name) return reader.ReadElementContentAsString();
                }
            }
            catch (XmlException)
            {
            }
            return null;
        }
    }
}
