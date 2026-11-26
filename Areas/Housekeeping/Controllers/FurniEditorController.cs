using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Implementations;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// The furni editor in Housekeeping (docs/furni-editor-housekeeping.md). The editor (dirplayer-rs habbo/furni-editor)
    /// runs in an iframe; this page opens casts for it and publishes the furni it makes.
    ///
    /// The rules: everything needs the fuse housekeeping_furni_editor, which no other fuse grants. Only furni made here are
    /// ever written: a sprite is new (no cast on the asset host, large or small, and no definition) or has versions here
    /// whose last casts are still the ones on disk; the hotel's own furni are opened read-only, to copy under a new name.
    /// Every publish is a version (furni_versions + furni-history/&lt;sprite&gt;/r&lt;n&gt;.cct), checked by
    /// FurniCastValidator before anything is written; a roll back publishes an earlier version again as a new one.
    /// Settings keplercms:furniEditor: url (the editor), castDir (hof_furni, read-write) and historyDir (the versions; both
    /// needed for publishing), maxBytes, castUrl.
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.housekeeping_furni_editor)]
    public class FurniEditorController : Controller
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };
        private static readonly string UploadDir = Path.Combine(Path.GetTempPath(), "furni-editor-uploads");
        /// <summary>Sprites that aren't free names: s_x is the small cast of x, poster casts are numbered by the catalogue.</summary>
        private static readonly Regex Reserved = new("^(s_|poster)", RegexOptions.IgnoreCase);

        private readonly IConfiguration _config;
        private readonly ICatalogueService _catalogue;
        private readonly ICommandQueueService _commands;
        private readonly IAuditLogService _audit;
        private readonly IGameDataService _gameData;
        private readonly DataContext _context;
        private readonly ICatalogueFurniService _give;

        public FurniEditorController(IConfiguration config, ICatalogueService catalogue, ICommandQueueService commands, IAuditLogService audit, IGameDataService gameData, DataContext context, ICatalogueFurniService give)
        {
            _give = give;
            _config = config;
            _catalogue = catalogue;
            _commands = commands;
            _audit = audit;
            _gameData = gameData;
            _context = context;
        }

        private IConfigurationSection Settings => _config.GetSection("keplercms:furniEditor");
        public const string DefaultEditorUrl = "https://webbanditten.github.io/dirplayer-rs/furni-editor/";
        private string EditorUrl => string.IsNullOrWhiteSpace(Settings["url"]) ? DefaultEditorUrl : Settings["url"];
        private string CastDir => Settings["castDir"];
        private string HistoryDir => Settings["historyDir"];
        private int MaxBytes => int.TryParse(Settings["maxBytes"], out var m) && m > 0 ? m : 3 * 1024 * 1024;
        private string CastUrl => string.IsNullOrWhiteSpace(Settings["castUrl"]) ? "https://images.habbo-danmark.com/hof_furni/" : Settings["castUrl"];
        private bool CanPublish => !string.IsNullOrWhiteSpace(CastDir) && Directory.Exists(CastDir) && !string.IsNullOrWhiteSpace(HistoryDir);
        private int UserId => int.Parse(User.Identity.Name);

        private static string CastFile(string sprite, bool small = false) => $"hh_furni_xx_{(small ? "s_" : "")}{sprite}.cct";
        private string LivePath(string sprite, bool small = false) => Path.Combine(CastDir, CastFile(sprite, small));
        private string VersionPath(string sprite, int revision, bool small = false) => Path.Combine(HistoryDir, sprite, $"r{revision}{(small ? "_s" : "")}.cct");
        private static string Sha256(byte[] data) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(data)).ToLowerInvariant();
        private static string Clean(string sprite) => (sprite ?? "").Split('*')[0].Trim();

        public IActionResult Index()
        {
            ViewData["editorUrl"] = EditorUrl;
            ViewData["editorOrigin"] = new Uri(EditorUrl).GetLeftPart(UriPartial.Authority);
            ViewData["canPublish"] = CanPublish;
            ViewData["maxBytes"] = MaxBytes;
            return View();
        }

        /// <summary>Definitions by sprite, name or id (and their colour variants, sprite "x*n"), marked when made here.</summary>
        [HttpGet]
        public async Task<IActionResult> Definitions(string query)
        {
            var list = await _catalogue.SearchFurni(query, 40);
            var sprites = list.Select(d => Clean(d.Sprite)).Distinct().ToList();
            var made = await _context.FurniVersions.Where(v => sprites.Contains(v.Sprite)).Select(v => v.Sprite).Distinct().ToListAsync();
            return Json(list.Select(d => new
            {
                d.Id, d.Sprite, d.Name, d.Description, d.Colour, d.Length, d.Width, d.Behaviour, d.Interactor, d.TopHeight, d.MaxStatus,
                madeHere = made.Contains(Clean(d.Sprite)),
            }));
        }

        // --- after publishing: a furni made here into the catalogue, or into your own hand to try it ---

        /// <summary>The catalogue's pages, to put a furni made here on one.</summary>
        [HttpGet]
        public async Task<IActionResult> CataloguePages() =>
            Json((await _catalogue.GetPages()).Select(p => new { id = p.Id, name = string.IsNullOrWhiteSpace(p.Name) ? p.NameIndex : p.Name, products = p.ProductCount, visible = p.IndexVisible, fuse = p.Fuse }));

        public class AfterRequest
        {
            public string Sprite { get; set; }
            public int PageId { get; set; }
            public int Price { get; set; }
        }

        /// <summary>The definition of a furni made here that is live, or the reason there is none.</summary>
        private async Task<(ItemsDefinitions definition, string problem)> MadeHere(string sprite)
        {
            sprite = Clean(sprite);
            if (string.IsNullOrEmpty(sprite)) return (null, "No furni.");
            var own = await Check(sprite);
            if (own.State != "mine" || own.Definition == null) return (null, $"'{sprite}' isn't a furni published from this editor with a definition: publish it first.");
            var definition = await _context.ItemsDefinitions.FirstOrDefaultAsync(d => d.Id == own.Definition.Id);
            return definition == null ? (null, "Its definition is gone.") : (definition, null);
        }

        /// <summary>Put a furni made here on a catalogue page as a new product, and tell the hotel.</summary>
        [HttpPost]
        public async Task<IActionResult> AddToCatalogue([FromBody] AfterRequest request)
        {
            var (definition, problem) = await MadeHere(request?.Sprite);
            if (problem != null) return BadRequest(problem);
            if (request.Price < 0 || request.Price > 9999) return BadRequest("A price from 0 to 9999 credits.");
            var page = (await _catalogue.GetPages()).FirstOrDefault(p => p.Id == request.PageId);
            if (page == null) return BadRequest("Pick a catalogue page.");
            var ids = await _catalogue.AddProducts(page.Id, new List<int> { definition.Id });
            if (ids.Count == 0) return BadRequest("The catalogue didn't take it.");
            var item = await _context.CatalogueItems.FirstAsync(i => i.Id == ids[0]);
            item.Price = request.Price;
            await _context.SaveChangesAsync();
            _commands.QueueCommand(CommandQueueType.reload_catalogue, new CommandTemplate());
            var pageName = string.IsNullOrWhiteSpace(page.Name) ? page.NameIndex : page.Name;
            await _audit.AddLog(AuditLogType.catalogue_furni_cast, UserId, definition.Sprite, $"On catalogue page {page.Id} ({pageName}) for {request.Price} credits, catalogue item {item.Id}", dataId: definition.Id);
            return Json(new { id = item.Id, page = pageName, price = item.Price });
        }

        /// <summary>One of a furni made here into your own hand, to try it in a room. It needs no catalogue item.</summary>
        [HttpPost]
        public async Task<IActionResult> GiveMe([FromBody] AfterRequest request)
        {
            var (definition, problem) = await MadeHere(request?.Sprite);
            if (problem != null) return BadRequest(problem);
            var me = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == UserId);
            if (me == null) return Forbid();
            var sql = new ContextSql(_context);
            await sql.InConnection(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                await _give.Give(me.Id, me.Username, new CatalogueFurniPart { Definition = definition, SpecialSpriteId = 0, Amount = 1 });
                await transaction.CommitAsync();
                return true;
            });
            _commands.QueueCommand(CommandQueueType.reload_inventory, new CommandTemplate { UserId = me.Id });
            await _audit.AddLog(AuditLogType.give_furni, UserId, "Gave 1 x " + definition.Sprite + " to " + me.Username, "from the furni editor, to try it", me.Id, definition.Id);
            return Json(new { ok = true });
        }

        /// <summary>The furni made here, newest first.</summary>
        [HttpGet]
        public async Task<IActionResult> Made()
        {
            var rows = await _context.FurniVersions.Select(v => new { v.Sprite, v.Revision, v.CreatedAt, v.DefinitionId }).ToListAsync();
            return Json(rows.GroupBy(v => v.Sprite).Select(g => g.OrderByDescending(v => v.Revision).First())
                .OrderByDescending(v => v.CreatedAt).Take(200)
                .Select(v => new { sprite = v.Sprite, revision = v.Revision, createdAt = v.CreatedAt, definitionId = v.DefinitionId }));
        }

        /// <summary>A cast for the editor: a version made here (revision), the live file on disk, or the asset host's.
        /// 404 when there is none (a furni without a small cast).</summary>
        [HttpGet]
        public async Task<IActionResult> Cast(string sprite, bool small = false, int revision = 0)
        {
            sprite = Clean(sprite);
            if (!FurniCastValidator.SpritePattern.IsMatch(sprite)) return BadRequest("Bad sprite");
            if (revision > 0)
            {
                if (string.IsNullOrWhiteSpace(HistoryDir)) return NotFound();
                var path = VersionPath(sprite, revision, small);
                return System.IO.File.Exists(path) ? File(await System.IO.File.ReadAllBytesAsync(path), "application/octet-stream") : NotFound();
            }
            if (!string.IsNullOrWhiteSpace(CastDir) && Directory.Exists(CastDir))
            {
                var path = LivePath(sprite, small);
                if (System.IO.File.Exists(path)) return File(await System.IO.File.ReadAllBytesAsync(path), "application/octet-stream");
            }
            var res = await Http.GetAsync(_gameData.InternalUrl(CastUrl.TrimEnd('/') + "/" + CastFile(sprite, small)));
            if (!res.IsSuccessStatusCode) return NotFound($"No cast {CastFile(sprite, small)} ({(int)res.StatusCode})");
            return File(await res.Content.ReadAsByteArrayAsync(), "application/octet-stream");
        }

        /// <summary>Whether a sprite may be published, and why not.</summary>
        private class Ownership
        {
            /// <summary>new | mine | taken</summary>
            public string State { get; set; }
            public string Reason { get; set; }
            public FurniVersion Latest { get; set; }
            public CatalogueFurni Definition { get; set; }
        }

        private async Task<Ownership> Check(string sprite)
        {
            var latest = await _context.FurniVersions.AsNoTracking().Where(v => v.Sprite == sprite).OrderByDescending(v => v.Revision).FirstOrDefaultAsync();
            var definitions = (await _catalogue.SearchFurni(sprite, 80)).Where(d => Clean(d.Sprite) == sprite).ToList();
            var large = LivePath(sprite);
            var small = LivePath(sprite, true);
            if (latest == null)
            {
                if (Reserved.IsMatch(sprite))
                    return new Ownership { State = "taken", Reason = $"'{sprite}' is reserved (s_ names the small cast of a furni, poster casts are numbered by the catalogue): pick another name" };
                if (System.IO.File.Exists(large) || System.IO.File.Exists(small))
                    return new Ownership { State = "taken", Reason = $"The hotel already has {CastFile(sprite, !System.IO.File.Exists(large))} and it wasn't made in this editor. Hotel furni are never written over: use \"New furni from this…\" in the editor to copy it under a new name." };
                if (definitions.Any())
                    return new Ownership { State = "taken", Reason = $"items_definitions already has '{definitions[0].Sprite}' (id {definitions[0].Id}), not made in this editor: pick another name" };
                return new Ownership { State = "new" };
            }
            // Made here: the casts on disk must still be the ones the last version wrote.
            var changed = new List<string>();
            if (!System.IO.File.Exists(large) || Sha256(await System.IO.File.ReadAllBytesAsync(large)) != latest.LargeSha256) changed.Add(CastFile(sprite));
            var smallSha = System.IO.File.Exists(small) ? Sha256(await System.IO.File.ReadAllBytesAsync(small)) : null;
            if (smallSha != latest.SmallSha256) changed.Add(CastFile(sprite, true));
            if (changed.Any())
                return new Ownership { State = "taken", Reason = $"{string.Join(" and ", changed)} changed outside the editor since r{latest.Revision}, so it isn't written over. Put it back (or ask whoever changed it) first.", Latest = latest };
            var others = definitions.Where(d => d.Id != latest.DefinitionId).ToList();
            if (others.Any())
                return new Ownership { State = "taken", Reason = $"items_definitions has '{sprite}' rows not made in this editor (id {string.Join(", ", others.Select(d => d.Id))})", Latest = latest };
            return new Ownership { State = "mine", Latest = latest, Definition = definitions.FirstOrDefault(d => d.Id == latest.DefinitionId) };
        }

        private class Upload
        {
            public string Sprite { get; set; }
            public int UserId { get; set; }
            public string LargeSha256 { get; set; }
            public string SmallSha256 { get; set; }
        }

        private static object ReportJson(FurniCastValidator.Report r) => r == null ? null : new
        {
            ok = r.Ok, r.Errors, r.Warnings, r.Bytes, r.Sha256, r.Bitmaps, r.Fields, r.Palettes, r.Classes, r.Length, r.Width,
        };

        /// <summary>Check a furni (multipart: sprite, cast, optional small) and keep it for Publish: the reports, whether the
        /// sprite may be published (new or made here) and an upload id when everything passes.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(16 * 1024 * 1024)]
        public async Task<IActionResult> Validate(string sprite, IFormFile cast, IFormFile small)
        {
            sprite = (sprite ?? "").Trim();
            if (cast == null || cast.Length == 0) return BadRequest(new { error = "No cast" });
            if (!CanPublish) return BadRequest(new { error = "Publishing is off: keplercms:furniEditor:castDir and historyDir aren't set" });
            if (cast.Length > MaxBytes || (small?.Length ?? 0) > MaxBytes) return BadRequest(new { error = $"A cast is larger than {MaxBytes:N0} bytes" });
            static async Task<byte[]> Read(IFormFile f)
            {
                await using var ms = new MemoryStream();
                await f.CopyToAsync(ms);
                return ms.ToArray();
            }
            var largeBytes = await Read(cast);
            var smallBytes = small is { Length: > 0 } ? await Read(small) : null;
            var largeReport = FurniCastValidator.Validate(largeBytes, sprite, MaxBytes);
            var smallReport = smallBytes == null ? null : FurniCastValidator.Validate(smallBytes, sprite, MaxBytes, small: true);
            var owner = FurniCastValidator.SpritePattern.IsMatch(sprite) ? await Check(sprite) : new Ownership { State = "taken", Reason = "Bad sprite" };
            var ok = largeReport.Ok && (smallReport?.Ok ?? true) && owner.State != "taken";
            string uploadId = null;
            if (ok)
            {
                Directory.CreateDirectory(UploadDir);
                foreach (var old in new DirectoryInfo(UploadDir).GetFiles().Where(f => f.LastWriteTimeUtc < DateTime.UtcNow.AddHours(-2))) old.Delete();
                uploadId = Guid.NewGuid().ToString("N");
                await System.IO.File.WriteAllBytesAsync(Path.Combine(UploadDir, uploadId + ".cct"), largeBytes);
                if (smallBytes != null) await System.IO.File.WriteAllBytesAsync(Path.Combine(UploadDir, uploadId + "_s.cct"), smallBytes);
                await System.IO.File.WriteAllTextAsync(Path.Combine(UploadDir, uploadId + ".json"),
                    JsonSerializer.Serialize(new Upload { Sprite = sprite, UserId = UserId, LargeSha256 = largeReport.Sha256, SmallSha256 = smallReport?.Sha256 }));
            }
            return Json(new
            {
                ok,
                state = owner.State,
                reason = owner.Reason,
                revision = (owner.Latest?.Revision ?? 0) + 1,
                keepsSmall = smallBytes == null && owner.Latest?.SmallSha256 != null,
                large = ReportJson(largeReport),
                small = ReportJson(smallReport),
                uploadId,
                definition = owner.Definition,
            });
        }

        public class PublishRequest
        {
            public string UploadId { get; set; }
            public bool SaveDefinition { get; set; }
            public CatalogueFurni Definition { get; set; }
            public string Note { get; set; }
        }

        /// <summary>Publish a validated furni as its next version. Without a small cast it keeps the current small one.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish([FromBody] PublishRequest request)
        {
            if (!CanPublish) return BadRequest(new { error = "Publishing is off" });
            if (string.IsNullOrEmpty(request?.UploadId) || !Regex.IsMatch(request.UploadId, "^[a-f0-9]{32}$")) return BadRequest(new { error = "No upload" });
            var basePath = Path.Combine(UploadDir, request.UploadId);
            if (!System.IO.File.Exists(basePath + ".cct") || !System.IO.File.Exists(basePath + ".json")) return BadRequest(new { error = "The upload has expired; check it again" });
            var meta = JsonSerializer.Deserialize<Upload>(await System.IO.File.ReadAllTextAsync(basePath + ".json"));
            if (meta.UserId != UserId) return Forbid();
            var large = await System.IO.File.ReadAllBytesAsync(basePath + ".cct");
            var small = System.IO.File.Exists(basePath + "_s.cct") ? await System.IO.File.ReadAllBytesAsync(basePath + "_s.cct") : null;
            if (Sha256(large) != meta.LargeSha256 || (small == null ? null : Sha256(small)) != meta.SmallSha256) return BadRequest(new { error = "The upload changed; check it again" });
            var result = await PublishVersion(meta.Sprite, large, small, keepSmall: true, request.SaveDefinition ? request.Definition : null, "editor", null, request.Note);
            foreach (var f in new[] { ".cct", "_s.cct", ".json" }) if (System.IO.File.Exists(basePath + f)) System.IO.File.Delete(basePath + f);
            return result;
        }

        /// <summary>
        /// Write one version: checks again (casts and ownership), reserves the revision (unique sprite + revision), keeps
        /// the casts in the history, writes the live files (temporary file + rename), saves the definition and asks Odin to
        /// reload. Each version is a whole furni: without a small cast, keepSmall carries the current one over, otherwise the
        /// live small cast (made here, Check compared it) goes.
        /// </summary>
        private async Task<IActionResult> PublishVersion(string sprite, byte[] large, byte[] small, bool keepSmall, CatalogueFurni definition, string source, int? rolledBackFrom, string note)
        {
            var largeReport = FurniCastValidator.Validate(large, sprite, MaxBytes);
            var smallReport = small == null ? null : FurniCastValidator.Validate(small, sprite, MaxBytes, small: true);
            if (!largeReport.Ok || smallReport is { Ok: false })
                return BadRequest(new { error = "The furni didn't pass the check", errors = largeReport.Errors.Concat(smallReport?.Errors ?? new List<string>()) });
            var owner = await Check(sprite);
            if (owner.State == "taken") return BadRequest(new { error = owner.Reason });
            if (owner.State == "new" && definition == null) return BadRequest(new { error = "A new furni needs its definition" });
            if (small == null && keepSmall && owner.Latest?.SmallSha256 != null) small = await System.IO.File.ReadAllBytesAsync(LivePath(sprite, true));

            note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            var version = new FurniVersion
            {
                Sprite = sprite,
                Revision = (owner.Latest?.Revision ?? 0) + 1,
                LargeSha256 = largeReport.Sha256,
                LargeBytes = large.Length,
                SmallSha256 = small == null ? null : Sha256(small),
                SmallBytes = small?.Length,
                DefinitionId = owner.Latest?.DefinitionId,
                Definition = owner.Latest?.Definition,
                UserId = UserId,
                CreatedAt = DateTime.Now,
                Source = source,
                RolledBackFrom = rolledBackFrom,
                Note = note?[..Math.Min(note.Length, 255)],
            };
            _context.FurniVersions.Add(version);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { error = "Someone published this furni at the same moment; check it again" });
            }

            var wroteLive = false;
            try
            {
                Directory.CreateDirectory(Path.Combine(HistoryDir, sprite));
                await WriteNew(VersionPath(sprite, version.Revision), large);
                if (small != null) await WriteNew(VersionPath(sprite, version.Revision, true), small);
                if (definition != null)
                {
                    definition.Sprite = sprite;
                    definition.Id = owner.Definition?.Id ?? 0;
                    definition.SpriteId = owner.Definition?.SpriteId ?? await _catalogue.MaxSpriteId() + 1;
                    version.DefinitionId = await _catalogue.SaveFurni(definition);
                    definition.OnPages = new List<int>();
                    version.Definition = JsonSerializer.Serialize(definition);
                    await _context.SaveChangesAsync();
                }
                wroteLive = true;
                await WriteLive(LivePath(sprite), large);
                if (small != null) await WriteLive(LivePath(sprite, true), small);
                else if (System.IO.File.Exists(LivePath(sprite, true))) System.IO.File.Delete(LivePath(sprite, true));
            }
            catch (Exception e)
            {
                if (wroteLive)
                {
                    // Half written: keep the version row (it matches what may now be live) and say so.
                    return StatusCode(500, new { error = $"Writing r{version.Revision}'s casts failed: {e.Message}. Check the files of {sprite}." });
                }
                // Nothing is live yet: forget this revision.
                _context.FurniVersions.Remove(version);
                await _context.SaveChangesAsync();
                foreach (var p in new[] { VersionPath(sprite, version.Revision), VersionPath(sprite, version.Revision, true) })
                    if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
                return StatusCode(500, new { error = "Publishing failed: " + e.Message });
            }

            _commands.QueueCommand(CommandQueueType.reload_catalogue, new CommandTemplate());
            var what = $"{sprite} r{version.Revision}: {CastFile(sprite)} {large.Length} bytes sha256 {version.LargeSha256}" +
                (small != null ? $", {CastFile(sprite, true)} {small.Length} bytes" : "") +
                (rolledBackFrom != null ? $", rolled back to r{rolledBackFrom}" : "") +
                (version.DefinitionId != null ? $", definition {version.DefinitionId}" : "");
            await _audit.AddLog(rolledBackFrom != null ? AuditLogType.restore_furni_cast : AuditLogType.publish_furni_cast, UserId, sprite, what, dataId: version.DefinitionId ?? 0);
            return Json(new { ok = true, sprite, revision = version.Revision, file = CastFile(sprite), small = small != null, definitionId = version.DefinitionId });
        }

        private static async Task WriteNew(string path, byte[] data)
        {
            await using var f = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            await f.WriteAsync(data);
        }

        private static async Task WriteLive(string path, byte[] data)
        {
            var temp = Path.Combine(Path.GetDirectoryName(path)!, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
            await System.IO.File.WriteAllBytesAsync(temp, data);
            System.IO.File.Move(temp, path, true);
        }

        /// <summary>A furni's versions, newest first.</summary>
        [HttpGet]
        public async Task<IActionResult> Versions(string sprite)
        {
            sprite = Clean(sprite);
            var rows = await _context.FurniVersions.AsNoTracking().Where(v => v.Sprite == sprite).OrderByDescending(v => v.Revision).ToListAsync();
            var userIds = rows.Select(v => v.UserId).Distinct().ToList();
            var names = await _context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username);
            return Json(rows.Select(v => new
            {
                v.Revision, v.CreatedAt, v.Source, v.RolledBackFrom, v.Note, v.LargeBytes, v.SmallBytes, v.DefinitionId,
                hasDefinition = v.Definition != null,
                user = names.TryGetValue(v.UserId, out var n) ? n : "#" + v.UserId,
            }));
        }

        public class RollbackRequest
        {
            public string Sprite { get; set; }
            public int Revision { get; set; }
            public bool WithDefinition { get; set; }
        }

        /// <summary>Publish an earlier version again, exactly as it was (both casts, optionally its definition), as a new revision.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rollback([FromBody] RollbackRequest request)
        {
            if (!CanPublish) return BadRequest(new { error = "Publishing is off" });
            var sprite = Clean(request?.Sprite);
            var version = await _context.FurniVersions.AsNoTracking().FirstOrDefaultAsync(v => v.Sprite == sprite && v.Revision == request.Revision);
            if (version == null) return NotFound(new { error = "No such version" });
            var largePath = VersionPath(sprite, version.Revision);
            var smallPath = VersionPath(sprite, version.Revision, true);
            if (!System.IO.File.Exists(largePath)) return BadRequest(new { error = $"r{version.Revision}'s cast isn't in the history folder" });
            var large = await System.IO.File.ReadAllBytesAsync(largePath);
            var small = System.IO.File.Exists(smallPath) ? await System.IO.File.ReadAllBytesAsync(smallPath) : null;
            if (Sha256(large) != version.LargeSha256 || (small == null ? null : Sha256(small)) != version.SmallSha256)
                return BadRequest(new { error = $"The kept casts of r{version.Revision} don't match what was published" });
            var definition = request.WithDefinition && version.Definition != null ? JsonSerializer.Deserialize<CatalogueFurni>(version.Definition) : null;
            return await PublishVersion(sprite, large, small, keepSmall: false, definition, "rollback", version.Revision, null);
        }
    }
}
