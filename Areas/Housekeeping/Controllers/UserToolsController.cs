using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// The tools on a Habbo's page in Housekeeping (Users/Manage), after Sulake's user data editor, Habbo restore tool, purse
    /// viewer, give furniture tool, subscription admin and the info tool's notes: edit motto and e-mail, send a password reset
    /// e-mail, give or take credits, give or end Habbo Club (Odin's club_days), give furni, staff notes; the page also shows the
    /// purse (Odin's credit_log) and the Habbo's pets. Credits, Habbo Club and furni need fuse_administrator_access.
    /// </summary>
    [Area("Housekeeping")]
    public class UserToolsController : Controller
    {
        public const int NoteMaxLength = 2000;
        public static readonly int[] ClubDays = { 7, 31, 93, 186, 372 };

        private readonly DataContext _context;
        private readonly IAuditLogService _audit;
        private readonly ICommandQueueService _commandQueue;
        private readonly IUserService _users;
        private readonly IMailService _mail;
        private readonly ICatalogueFurniService _catalogue;
        private readonly IFurniShopService _shop;
        private readonly ILogger<UserToolsController> _logger;

        public UserToolsController(DataContext context, IAuditLogService audit, ICommandQueueService commandQueue, IUserService users, IMailService mail,
            ICatalogueFurniService catalogue, IFurniShopService shop, ILogger<UserToolsController> logger)
        {
            _context = context;
            _audit = audit;
            _commandQueue = commandQueue;
            _users = users;
            _mail = mail;
            _catalogue = catalogue;
            _shop = shop;
            _logger = logger;
        }

        public record NoteRow(int Id, int StaffId, string Staff, string Note, DateTime CreatedAt);
        public record PurseRow(DateTime At, string Type, int Credits);
        public record PetRow(int ItemId, string Name, string Type, int Race, string Colour, DateTime Born, int RoomId, string RoomName);

        /// <summary>What the user page shows besides the user itself.</summary>
        public class UserExtras
        {
            public List<NoteRow> Notes { get; set; } = new();
            public List<PurseRow> Purse { get; set; } = new();
            public List<PetRow> Pets { get; set; } = new();
            public DateTime? ClubUntil { get; set; }
        }

        public static async Task<UserExtras> Load(DataContext context, int userId)
        {
            var sql = new ContextSql(context);
            var extras = new UserExtras();
            extras.Notes = await (from n in context.UserNotes.AsNoTracking()
                                  join u in context.Users.AsNoTracking() on n.StaffId equals u.Id into staff
                                  from s in staff.DefaultIfEmpty()
                                  where n.UserId == userId
                                  orderby n.Id descending
                                  select new NoteRow(n.Id, n.StaffId, s != null ? s.Username : null, n.Note, n.CreatedAt)).ToListAsync();
            extras.Purse = await sql.QueryOpen("SELECT timestamp, type, credits FROM credit_log WHERE user_id = @user ORDER BY id DESC LIMIT 200",
                r => new PurseRow(DateTimeOffset.FromUnixTimeSeconds(r.GetInt64(0)).LocalDateTime, r.GetString(1), r.GetInt32(2)), ("@user", userId));
            extras.Pets = await sql.QueryOpen(
                "SELECT p.item_id, p.name, p.type, p.race, p.colour, p.born, i.room_id, r.name FROM items_pets p JOIN items i ON i.id = p.item_id " +
                "LEFT JOIN rooms r ON r.id = i.room_id WHERE i.user_id = @user ORDER BY p.born DESC",
                r => new PetRow((int)r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetInt32(3), r.IsDBNull(4) ? "" : r.GetString(4),
                    DateTimeOffset.FromUnixTimeSeconds(r.GetInt64(5)).LocalDateTime, r.GetInt32(6), r.IsDBNull(7) ? null : r.GetString(7)), ("@user", userId));
            var expiration = await context.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.ClubExpiration).FirstOrDefaultAsync();
            if (expiration > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) extras.ClubUntil = DateTimeOffset.FromUnixTimeSeconds((long)expiration).LocalDateTime;
            return extras;
        }

        private int Me => int.Parse(User.Identity.Name);

        private IActionResult Back(int id, string message) => RedirectToAction("Manage", "Users", new { id, message });

        /// <summary>Motto (as the profile page cleans it) and e-mail.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.fuse_kick)]
        public async Task<IActionResult> Details(int id, string motto, string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            motto = new string((motto ?? "").Where(c => c >= ' ').ToArray()).Trim();
            if (motto.Length > KeplerCMS.Controllers.ProfileController.MissionMaxLength) motto = motto[..KeplerCMS.Controllers.ProfileController.MissionMaxLength];
            email = (email ?? "").Trim();
            // Checked only when changed: older accounts can have addresses the website's rule wouldn't take today.
            if (email != (user.Email ?? "") && email.Length > 0 && (email.Length > 256 || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")))
                return Back(id, "That isn't an e-mail address.");

            var changes = new List<string>();
            if (motto != (user.Motto ?? "")) changes.Add("motto \"" + (user.Motto ?? "") + "\" -> \"" + motto + "\"");
            if (email != (user.Email ?? "")) changes.Add("e-mail changed");
            if (changes.Count == 0) return Back(id, "Nothing changed.");

            user.Motto = motto;
            user.Email = email;
            await _context.SaveChangesAsync();
            if (changes.Any(c => c.StartsWith("motto")))
                _commandQueue.QueueCommand(CommandQueueType.refresh_appearance, new CommandTemplate { UserId = id });
            await _audit.AddLog(AuditLogType.edit_user, Me, "Edited " + user.Username, Trim(string.Join("; ", changes)), id);
            return Back(id, "Saved: " + string.Join(", ", changes.Select(c => c.StartsWith("motto") ? "motto" : c)) + ".");
        }

        /// <summary>The website's forgotten-password e-mail (a reset link) to the Habbo's address, as the Habbo restore tool did.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.fuse_kick)]
        public async Task<IActionResult> PasswordReset(int id)
        {
            var user = await _context.Users.AsNoTracking().Where(u => u.Id == id).Select(u => new { u.Id, u.Username, u.Email }).FirstOrDefaultAsync();
            if (user == null) return NotFound();
            if (string.IsNullOrWhiteSpace(user.Email)) return Back(id, "This Habbo has no e-mail address: add one first.");

            var link = await _users.GeneratePasswordResetLink(user.Id);
            try
            {
                var sent = await _mail.SendForgotPassword(user.Email, link);
                if (!sent.Successful) return Back(id, "The e-mail could not be sent (" + string.Join(", ", sent.ErrorMessages) + ").");
            }
            catch (Exception e)
            {
                _logger.LogWarning("Password reset mail for user {Id} failed ({Message})", id, e.Message);
                return Back(id, "The e-mail could not be sent.");
            }
            await _audit.AddLog(AuditLogType.password_reset_mail, Me, "Sent a password reset e-mail to " + user.Username, null, id);
            return Back(id, "Password reset e-mail sent to " + user.Email + ".");
        }

        /// <summary>Gives (amount &gt; 0) or takes credits; an online Habbo's purse follows at once.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Credits(int id, int amount, string reason)
        {
            if (amount == 0 || Math.Abs(amount) > 1000000) return Back(id, "Give between 1 and 1,000,000 credits, or take them with a minus.");
            var username = await _context.Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.Username).FirstOrDefaultAsync();
            if (username == null) return NotFound();

            var changed = await _context.Users.Where(u => u.Id == id && u.Credits + amount >= 0).ExecuteUpdateAsync(s => s.SetProperty(u => u.Credits, u => u.Credits + amount));
            if (changed == 0) return Back(id, "The Habbo doesn't have that many credits.");

            _commandQueue.QueueCommand(amount > 0 ? CommandQueueType.update_credits : CommandQueueType.reduce_credits, new CommandTemplate { UserId = id, Credits = Math.Abs(amount) });
            await _audit.AddLog(AuditLogType.give_credits, Me, (amount > 0 ? "Gave " + amount + " credits to " : "Took " + -amount + " credits from ") + username, Trim(reason), id, amount);
            return Back(id, amount > 0 ? "Gave " + amount + " credits." : "Took " + -amount + " credits.");
        }

        /// <summary>Gives days of Habbo Club (as a purchase, without the price) or ends the membership now; the hotel applies it.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Club(int id, int days, bool end = false, string reason = null)
        {
            var username = await _context.Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.Username).FirstOrDefaultAsync();
            if (username == null) return NotFound();
            if (!end && (days < 1 || days > 3650)) return Back(id, "Give between 1 and 3,650 days.");

            _commandQueue.QueueCommand(CommandQueueType.club_days, new CommandTemplate { UserId = id, Days = end ? 0 : days });
            await _audit.AddLog(AuditLogType.give_club, Me, end ? "Ended the Habbo Club membership of " + username : "Gave " + days + " days of Habbo Club to " + username, Trim(reason), id, end ? 0 : days);
            return Back(id, end ? "Habbo Club ends now (the hotel applies it in a moment; reload to see it)." : "Gave " + days + " days of Habbo Club (the hotel applies it in a moment; reload to see it).");
        }

        /// <summary>Catalogue items to give, by name, sale code or id (hidden ones too).</summary>
        [HttpGet]
        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> FurniSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Json(Array.Empty<object>());
            var catalogue = await _catalogue.Load();
            var found = await _shop.Search(query);
            return Json(found.Take(30).Select(e => new
            {
                id = e.Id,
                name = e.Name,
                saleCode = e.SaleCode,
                sprite = e.ImageSprite,
                notGivable = catalogue.ById.TryGetValue(e.Id, out var full) ? full.NotGivable : "not in the catalogue",
            }));
        }

        /// <summary>Puts a catalogue item (a package gives all its parts) in the Habbo's hand, like a voucher; their hand reloads.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> GiveFurni(int id, int catalogueItemId, int amount = 1, string reason = null)
        {
            if (amount < 1 || amount > 50) return Back(id, "Give between 1 and 50.");
            var username = await _context.Users.AsNoTracking().Where(u => u.Id == id).Select(u => u.Username).FirstOrDefaultAsync();
            if (username == null) return NotFound();
            var catalogue = await _catalogue.Load();
            if (!catalogue.ById.TryGetValue(catalogueItemId, out var entry)) return Back(id, "Pick a catalogue item first.");
            if (entry.NotGivable != null) return Back(id, entry.Name + " can't be given: " + entry.NotGivable + ".");

            var sql = new ContextSql(_context);
            await sql.InConnection(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var film = 0;
                for (var n = 0; n < amount; n++)
                {
                    foreach (var part in entry.Parts)
                    {
                        for (var i = 0; i < part.Amount; i++) film += await _catalogue.Give(id, username, part);
                    }
                }
                if (film > 0) await sql.Execute("UPDATE users SET film = film + @film WHERE id = @user", ("@film", film), ("@user", id));
                await transaction.CommitAsync();
                return true;
            });

            _commandQueue.QueueCommand(CommandQueueType.reload_inventory, new CommandTemplate { UserId = id });
            await _audit.AddLog(AuditLogType.give_furni, Me, "Gave " + amount + " x " + entry.Name + " to " + username, Trim(reason), id, entry.Id);
            return Back(id, "Gave " + amount + " x " + entry.Name + ".");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.fuse_kick)]
        public async Task<IActionResult> AddNote(int id, string note)
        {
            note = (note ?? "").Trim();
            if (note.Length == 0) return Back(id, "Write the note first.");
            if (note.Length > NoteMaxLength) note = note[..NoteMaxLength];
            if (!await _context.Users.AnyAsync(u => u.Id == id)) return NotFound();

            _context.UserNotes.Add(new UserNote { UserId = id, StaffId = Me, Note = note, CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();
            await _audit.AddLog(AuditLogType.user_note, Me, "Added a staff note", Trim(note), id);
            return Back(id, "Note added.");
        }

        /// <summary>A note's writer deletes it; an administrator deletes any.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HousekeepingFilter(Fuse.fuse_kick)]
        public async Task<IActionResult> DeleteNote(int id, int noteId)
        {
            var note = await _context.UserNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.UserId == id);
            if (note == null) return Back(id, "That note is already gone.");
            var me = Me;
            if (note.StaffId != me)
            {
                var rank = await _context.Users.Where(u => u.Id == me).Select(u => (int)u.Rank).FirstOrDefaultAsync();
                var admin = await _context.RankRights.AnyAsync(r => r.RankId == rank && r.FuseName == "fuse_administrator_access");
                if (!admin) return Back(id, "Only its writer or an administrator can delete a note.");
            }
            _context.UserNotes.Remove(note);
            await _context.SaveChangesAsync();
            await _audit.AddLog(AuditLogType.user_note, me, "Deleted a staff note", Trim(note.Note), id);
            return Back(id, "Note deleted.");
        }

        /// <summary>The audit log's columns are 255 characters.</summary>
        private static string Trim(string s)
        {
            s = string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            return s != null && s.Length > 250 ? s[..250] + "…" : s;
        }
    }
}
