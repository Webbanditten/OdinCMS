using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Hobba tools > Customer service > Help tickets: the questions sent through the Habbo Help Tool (IotController). Staff
    /// read a ticket, answer it (the player gets an e-mail with the ticket's link when they left an address), and close or
    /// reopen it. Waiting = new or the player wrote again.
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.fuse_receive_calls_for_help)]
    public class HelpTicketsController : Controller
    {
        private readonly DataContext _context;
        private readonly IMailService _mail;
        private readonly IAuditLogService _audit;
        private readonly ILogger<HelpTicketsController> _logger;

        public HelpTicketsController(DataContext context, IMailService mail, IAuditLogService audit, ILogger<HelpTicketsController> logger)
        {
            _context = context;
            _mail = mail;
            _audit = audit;
            _logger = logger;
        }

        public class TicketRow
        {
            public HelpTicket Ticket { get; set; }
            public string Habbo { get; set; }
            public int Replies { get; set; }
        }

        public async Task<IActionResult> Index(string show = "waiting")
        {
            var query = _context.HelpTickets.AsNoTracking();
            query = show switch
            {
                "answered" => query.Where(t => t.Status == HelpTickets.Answered),
                "closed" => query.Where(t => t.Status == HelpTickets.Closed),
                "all" => query,
                _ => query.Where(t => t.Status == HelpTickets.New || t.Status == HelpTickets.PlayerReplied),
            };
            var tickets = await query.OrderByDescending(t => t.UpdatedAt).Take(200).ToListAsync();
            var userIds = tickets.Where(t => t.UserId != null).Select(t => t.UserId.Value).Distinct().ToList();
            var names = await _context.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username);
            var ids = tickets.Select(t => t.Id).ToList();
            var replies = await _context.HelpTicketResponses.AsNoTracking().Where(r => ids.Contains(r.TicketId)).GroupBy(r => r.TicketId)
                .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count);
            ViewBag.Show = show;
            ViewBag.Waiting = await _context.HelpTickets.CountAsync(t => t.Status == HelpTickets.New || t.Status == HelpTickets.PlayerReplied);
            return View(tickets.Select(t => new TicketRow
            {
                Ticket = t,
                Habbo = t.UserId != null && names.TryGetValue(t.UserId.Value, out var n) ? n : null,
                Replies = replies.GetValueOrDefault(t.Id),
            }).ToList());
        }

        public class TicketView
        {
            public HelpTicket Ticket { get; set; }
            public string Habbo { get; set; }
            public List<(HelpTicketResponse Response, string Staff)> Responses { get; set; }
            public string Message { get; set; }
        }

        public async Task<IActionResult> View(int id, string message = null)
        {
            var ticket = await _context.HelpTickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();
            var responses = await _context.HelpTicketResponses.AsNoTracking().Where(r => r.TicketId == id).OrderBy(r => r.Id).ToListAsync();
            var staffIds = responses.Where(r => r.StaffId != null).Select(r => r.StaffId.Value).Distinct().ToList();
            var staff = await _context.Users.AsNoTracking().Where(u => staffIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username);
            return View(new TicketView
            {
                Ticket = ticket,
                Habbo = ticket.UserId != null ? await _context.Users.AsNoTracking().Where(u => u.Id == ticket.UserId).Select(u => u.Username).FirstOrDefaultAsync() : null,
                Responses = responses.Select(r => (r, r.StaffId != null ? staff.GetValueOrDefault(r.StaffId.Value) : null)).ToList(),
                Message = message,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string message, bool close = false)
        {
            var ticket = await _context.HelpTickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();
            message = (message ?? "").Trim();
            if (message.Length == 0) return RedirectToAction("View", new { id, message = "Write an answer first." });

            var staffId = int.Parse(User.Identity.Name);
            _context.HelpTicketResponses.Add(new HelpTicketResponse { TicketId = id, StaffId = staffId, Message = message.Length > 4000 ? message.Substring(0, 4000) : message, CreatedAt = DateTime.Now });
            ticket.Status = close ? HelpTickets.Closed : HelpTickets.Answered;
            ticket.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await _audit.AddLog(AuditLogType.help_ticket_reply, staffId, "Help ticket #" + id, close ? "answered and closed" : "answered", ticket.UserId ?? 0, id);

            var note = "Answer saved.";
            if (!string.IsNullOrEmpty(ticket.Email))
            {
                var link = $"https://{Request.Host}/iot/ticket?id={id}&token={ticket.Token}";
                try
                {
                    var sent = await _mail.SendHelpTicketReply(ticket.Email, link);
                    note = sent.Successful ? "Answer saved and e-mailed to the player." : "Answer saved; the e-mail could not be sent (" + string.Join(", ", sent.ErrorMessages) + ").";
                }
                catch (Exception e)
                {
                    _logger.LogWarning("Help ticket {Id}: e-mail failed ({Message})", id, e.Message);
                    note = "Answer saved; the e-mail could not be sent.";
                }
            }
            else
            {
                note = "Answer saved. The player left no e-mail: they see it through their ticket link or Dine henvendelser.";
            }
            return RedirectToAction("View", new { id, message = note });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, bool closed)
        {
            var ticket = await _context.HelpTickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();
            ticket.Status = closed ? HelpTickets.Closed : HelpTickets.PlayerReplied;
            ticket.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await _audit.AddLog(AuditLogType.help_ticket_close, int.Parse(User.Identity.Name), "Help ticket #" + id, closed ? "closed" : "reopened", ticket.UserId ?? 0, id);
            return RedirectToAction("View", new { id, message = closed ? "Closed." : "Reopened." });
        }
    }
}
