using System;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Filters;
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
            public string Error { get; set; }
            public HelpTicket Ticket { get; set; }
            public System.Collections.Generic.List<HelpTicket> Mine { get; set; } = new();
        }

        private Users Me => ViewData["user"] as Users;

        [HttpGet("iot/go")]
        public async Task<IActionResult> Go()
        {
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
                    if (form.Event != "Member" && form.Event != "Not member") return Step(1, form, "Vælg et af svarene.");
                    form.Member = form.Event == "Member";
                    if (form.Member && Me != null)
                    {
                        form.Name ??= Me.Username;
                        form.Email ??= Clean(Me.Email, 255);
                    }
                    return Step(2, form);
                case 26: // who you are
                    if (form.Member && string.IsNullOrEmpty(form.Name)) return Step(2, form, "Skriv dit Habbo-navn.");
                    if (Me == null && !ValidEmail(form.Email)) return Step(2, form, "Skriv en e-mail-adresse, så vi kan give dig besked, når vi har svaret.");
                    if (!string.IsNullOrEmpty(form.Email) && !ValidEmail(form.Email)) return Step(2, form, "Den e-mail-adresse ser forkert ud.");
                    return Step(3, form);
                case 76: // the problem
                    if (!HelpTickets.Issues.Any(i => i.Key == form.Issue)) return Step(3, form, "Vælg hvad din henvendelse handler om.");
                    return Step(4, form);
                case 63: // describe it
                    if (!HelpTickets.Issues.Any(i => i.Key == form.Issue)) return Step(3, form, "Vælg hvad din henvendelse handler om.");
                    if (form.Message.Length < 10) return Step(4, form, "Beskriv dit problem med lidt flere ord.");
                    if (!string.IsNullOrEmpty(form.Website)) return Step(5, form); // a bot: pretend it went well

                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var since = DateTime.Now.AddHours(-1);
                    if (await _context.HelpTickets.CountAsync(t => t.Ip == ip && t.CreatedAt > since) >= MaxPerHour)
                        return Step(4, form, "Du har sendt mange henvendelser på kort tid. Vent lidt, og prøv igen.");

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
                    Error = ticket.Status == HelpTickets.Closed ? "Henvendelsen er lukket. Send en ny, hvis du stadig har brug for hjælp." : "Skriv dit svar først.",
                });
            }
            _context.HelpTicketResponses.Add(new HelpTicketResponse { TicketId = id, Message = message.Length > 4000 ? message.Substring(0, 4000) : message, CreatedAt = DateTime.Now });
            ticket.Status = HelpTickets.PlayerReplied;
            ticket.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Redirect($"/iot/ticket?id={id}&token={token}&sent=true");
        }
    }
}
