using System;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
using KeplerCMS.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Controllers
{
    /// <summary>
    /// The Habbo Help Tool ("IOT"), built like habbo.com's 2007 one (Views/Iot, wwwroot/iot from jeanropke/Neptune): step 1
    /// do you have a Habbo, step 2 your Habbo name and e-mail (or only e-mail), step 3 the problem, step 4 describe it. The
    /// answers travel in the form (no session). Sending creates a HelpTicket with a secret token; the player reads staff's
    /// answers and replies at /iot/ticket?id=ID&amp;token=TOKEN (linked on the done page, in "Dine henvendelser" for
    /// logged-in players and in the e-mail staff's answer sends). Staff answer in Housekeeping > Help tickets.
    /// Every text is in Helpers/IotTexts; staff with housekeeping_localization edit them on the pages (?edit=1, SaveTexts)
    /// and can open every step without sending anything (?preview=, see Preview).
    /// </summary>
    [LoggedInFilter(false)]
    public class IotController : Controller
    {
        public const int MaxPerHour = 5;
        private readonly DataContext _context;

        public IotController(DataContext context)
        {
            _context = context;
        }

        public class IotForm
        {
            public int Sid { get; set; }
            public string Event { get; set; }
            public bool Member { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Issue { get; set; }
            public string Message { get; set; }
            /// <summary>Honeypot: people never see it, spam bots fill it in.</summary>
            public string Website { get; set; }
            /// <summary>The error's text key (IotTexts "err_*").</summary>
            public string Error { get; set; }
            public HelpTicket Ticket { get; set; }
            public System.Collections.Generic.List<HelpTicket> Mine { get; set; } = new();
        }

        private Users Me => ViewData["user"] as Users;

        private bool Editing => IotTexts.CanEdit(Me) && Request.Cookies[IotTexts.EditCookie] == "1";

        [HttpGet("iot/go")]
        public async Task<IActionResult> Go(string edit = null, string preview = null)
        {
            // The text editor: staff switch it on and off with ?edit=1 / ?edit=0 (a cookie for this browser session).
            if (edit != null && IotTexts.CanEdit(Me))
            {
                if (edit == "1") Response.Cookies.Append(IotTexts.EditCookie, "1", new CookieOptions { Path = "/iot", HttpOnly = true, SameSite = SameSiteMode.Lax, Secure = Request.IsHttps });
                else Response.Cookies.Delete(IotTexts.EditCookie, new CookieOptions { Path = "/iot" });
                return Redirect("/iot/go");
            }
            if (preview != null && Editing) return Preview(preview);

            var form = new IotForm();
            if (Me != null)
            {
                form.Mine = await _context.HelpTickets.AsNoTracking().Where(t => t.UserId == Me.Id).OrderByDescending(t => t.Id).Take(10).ToListAsync();
            }
            return View("~/Views/Iot/Step1.cshtml", form);
        }

        [HttpPost("iot/go")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Next(IotForm form)
        {
            form.Name = Clean(form.Name, 50);
            form.Email = Clean(form.Email, 255);
            form.Message = (form.Message ?? "").Trim();
            if (form.Message.Length > 4000) form.Message = form.Message.Substring(0, 4000);

            switch (form.Sid)
            {
                case 58: // do you have a Habbo?
                    if (form.Event != "Member" && form.Event != "Not member") return Step(1, form, "err_choose");
                    form.Member = form.Event == "Member";
                    if (form.Member && Me != null)
                    {
                        form.Name ??= Me.Username;
                        form.Email ??= Clean(Me.Email, 255);
                    }
                    return Step(2, form);
                case 26: // who you are
                    if (form.Member && string.IsNullOrEmpty(form.Name)) return Step(2, form, "err_name");
                    if (Me == null && !ValidEmail(form.Email)) return Step(2, form, "err_email_needed");
                    if (!string.IsNullOrEmpty(form.Email) && !ValidEmail(form.Email)) return Step(2, form, "err_email_bad");
                    return Step(3, form);
                case 76: // the problem
                    if (!HelpTickets.Issues.Any(i => i.Key == form.Issue)) return Step(3, form, "err_issue");
                    return Step(4, form);
                case 63: // describe it
                    if (!HelpTickets.Issues.Any(i => i.Key == form.Issue)) return Step(3, form, "err_issue");
                    if (form.Message.Length < 10) return Step(4, form, "err_short");
                    if (!string.IsNullOrEmpty(form.Website)) return Step(5, form); // a bot: pretend it went well

                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var since = DateTime.Now.AddHours(-1);
                    if (await _context.HelpTickets.CountAsync(t => t.Ip == ip && t.CreatedAt > since) >= MaxPerHour)
                        return Step(4, form, "err_flood");

                    var ticket = new HelpTicket
                    {
                        UserId = Me?.Id,
                        Name = form.Member ? form.Name : null,
                        Email = form.Email,
                        Issue = form.Issue,
                        Message = form.Message,
                        Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
                        Status = HelpTickets.New,
                        Ip = ip,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                    };
                    _context.HelpTickets.Add(ticket);
                    await _context.SaveChangesAsync();
                    form.Ticket = ticket;
                    return Step(5, form);
                default:
                    return RedirectToAction("Go");
            }
        }

        private IActionResult Step(int step, IotForm form, string error = null)
        {
            form.Error = error;
            return View("~/Views/Iot/Step" + step + ".cshtml", form);
        }

        private static string Clean(string s, int length)
        {
            s = s?.Trim();
            if (string.IsNullOrEmpty(s)) return null;
            return s.Length > length ? s.Substring(0, length) : s;
        }

        private static bool ValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || email.Length > 255) return false;
            try { return new MailAddress(email).Address == email; }
            catch (FormatException) { return false; }
        }

        public class TicketPage
        {
            public HelpTicket Ticket { get; set; }
            public System.Collections.Generic.List<HelpTicketResponse> Responses { get; set; }
            public string Error { get; set; }
            public bool Sent { get; set; }
        }

        private async Task<HelpTicket> Find(int id, string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            var ticket = await _context.HelpTickets.FirstOrDefaultAsync(t => t.Id == id);
            return ticket != null && CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(ticket.Token), System.Text.Encoding.UTF8.GetBytes(token)) ? ticket : null;
        }

        [HttpGet("iot/ticket")]
        public async Task<IActionResult> Ticket(int id, string token, bool sent = false)
        {
            var ticket = await Find(id, token);
            if (ticket == null) return NotFound();
            return View("~/Views/Iot/Ticket.cshtml", new TicketPage
            {
                Ticket = ticket,
                Responses = await _context.HelpTicketResponses.AsNoTracking().Where(r => r.TicketId == id).OrderBy(r => r.Id).ToListAsync(),
                Sent = sent,
            });
        }

        [HttpPost("iot/ticket")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string token, string message)
        {
            var ticket = await Find(id, token);
            if (ticket == null) return NotFound();
            message = (message ?? "").Trim();
            if (ticket.Status == HelpTickets.Closed || message.Length < 2)
            {
                return View("~/Views/Iot/Ticket.cshtml", new TicketPage
                {
                    Ticket = ticket,
                    Responses = await _context.HelpTicketResponses.AsNoTracking().Where(r => r.TicketId == id).OrderBy(r => r.Id).ToListAsync(),
                    Error = ticket.Status == HelpTickets.Closed ? "err_closed" : "err_reply_empty",
                });
            }
            _context.HelpTicketResponses.Add(new HelpTicketResponse { TicketId = id, Message = message.Length > 4000 ? message.Substring(0, 4000) : message, CreatedAt = DateTime.Now });
            ticket.Status = HelpTickets.PlayerReplied;
            ticket.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Redirect($"/iot/ticket?id={id}&token={token}&sent=true");
        }

        public class BanPage
        {
            public bool LoggedIn { get; set; }
            public UsersBans Ban { get; set; }
        }

        /// <summary>"Er du smidt ud?" (step 1's shortcut): a logged-in player's own ban on the hotel and when it ends.</summary>
        [HttpGet("iot/ban")]
        public async Task<IActionResult> Ban()
        {
            var page = new BanPage { LoggedIn = Me != null };
            if (Me != null)
            {
                var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                page.Ban = await _context.UsersBans.AsNoTracking()
                    .Where(b => b.BanType == nameof(BanTypes.USER_ID) && b.UserId == Me.Id && b.BannedUntilTimestamp > now)
                    .OrderByDescending(b => b.BannedUntilTimestamp).FirstOrDefaultAsync();
            }
            return View("~/Views/Iot/Ban.cshtml", page);
        }

        /// <summary>The text editor's Save (Views/Iot/_TextEditor.cshtml): fields "t.KEY", then back to the page it came from.</summary>
        [HttpPost("iot/texts")]
        [ValidateAntiForgeryToken]
        public IActionResult SaveTexts(string back)
        {
            if (!IotTexts.CanEdit(Me)) return NotFound();
            var values = Request.Form.Keys.Where(k => k.StartsWith("t.")).ToDictionary(k => k.Substring(2), k => (string)Request.Form[k]);
            var changed = IotTexts.Save(values);
            if (string.IsNullOrEmpty(back) || !Url.IsLocalUrl(back) || !back.StartsWith("/iot/")) back = "/iot/go";
            return Redirect(back + (back.Contains('?') ? "&" : "?") + "saved=" + changed);
        }

        /// <summary>Every step and page with made-up answers, for the text editor (nothing is sent or saved).</summary>
        private IActionResult Preview(string page)
        {
            var form = new IotForm { Member = true, Name = Me.Username, Email = Me.Email, Issue = HelpTickets.Issues[0].Key };
            var ticket = new HelpTicket
            {
                Id = 0, Token = "preview", Name = Me.Username, Issue = form.Issue, Status = HelpTickets.Answered,
                Message = "(Eksempel) Jeg kan ikke logge ind.", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now,
            };
            var answer = new HelpTicketResponse { TicketId = 0, StaffId = Me.Id, Message = "(Eksempel) Prøv at få en ny adgangskode.", CreatedAt = DateTime.Now };
            switch (page)
            {
                case "2": return Step(2, form);
                case "2-guest": form.Member = false; form.Name = null; return Step(2, form);
                case "3": return Step(3, form);
                case "4": return Step(4, form);
                case "5": form.Ticket = ticket; return Step(5, form);
                case "ticket":
                case "ticket-closed":
                    if (page == "ticket-closed") ticket.Status = HelpTickets.Closed;
                    return View("~/Views/Iot/Ticket.cshtml", new TicketPage { Ticket = ticket, Responses = new() { answer }, Sent = page == "ticket" });
                case "ban":
                case "ban-none":
                case "ban-guest":
                    var ban = page == "ban" ? new UsersBans { UserId = Me.Id, Message = "(Eksempel) Snyd i et byt.", BannedUntilTimestamp = (int)DateTimeOffset.UtcNow.AddDays(3).ToUnixTimeSeconds() } : null;
                    return View("~/Views/Iot/Ban.cshtml", new BanPage { LoggedIn = page != "ban-guest", Ban = ban });
                default: return RedirectToAction("Go");
            }
        }
    }
}
