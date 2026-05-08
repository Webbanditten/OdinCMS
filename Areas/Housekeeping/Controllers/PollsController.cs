using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class PollsController : Controller
    {
        private readonly IPollService _pollService;
        private readonly IAuditLogService _auditService;

        public PollsController(IPollService pollService, IAuditLogService auditService)
        {
            _pollService = pollService;
            _auditService = auditService;
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Index()
        {
            var polls = await _pollService.GetAll();
            return View(polls);
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public IActionResult Create()
        {
            return View(new PollCreateViewModel());
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        [HttpPost]
        public async Task<IActionResult> Create(PollCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var poll = await _pollService.Create(model);
                await _auditService.AddLog(AuditLogType.create_poll, int.Parse(HttpContext.User.Identity.Name), null, null, 0, poll.Id);
                return RedirectToAction("Index", "Polls", new { message = "Poll created successfully" });
            }
            return View(model);
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Update(int id)
        {
            var poll = await _pollService.Get(id);
            if (poll == null) return RedirectToAction("Index", new { message = "Poll not found" });

            var model = new PollCreateViewModel
            {
                Headline = poll.Headline,
                Description = poll.Description,
                ThankYou = poll.ThankYou
            };

            // Map questions
            if (poll.Questions != null)
            {
                foreach (var q in poll.Questions)
                {
                    var qvm = new PollQuestionViewModel
                    {
                        Text = q.Text,
                        Type = q.Type,
                        MinSelect = q.MinSelect,
                        MaxSelect = q.MaxSelect,
                        Order = q.Order,
                        Options = new System.Collections.Generic.List<string>()
                    };

                    if (q.Options != null)
                    {
                        foreach (var opt in q.Options)
                        {
                            qvm.Options.Add(opt.Name);
                        }
                    }

                    model.Questions.Add(qvm);
                }
            }

            // Map trigger
            if (poll.Triggers != null && poll.Triggers.Count > 0)
            {
                var trigger = poll.Triggers[0];
                model.TriggerRoomId = trigger.Room;
                model.TriggerTimeFrom = trigger.TimeFrom;
                model.TriggerTimeTo = trigger.TimeTo;
            }

            ViewBag.PollId = id;
            return View(model);
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        [HttpPost]
        public async Task<IActionResult> Update(int id, PollCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _pollService.Update(id, model);
                await _auditService.AddLog(AuditLogType.edit_poll, int.Parse(HttpContext.User.Identity.Name), null, null, 0, id);
                return RedirectToAction("Index", "Polls", new { message = "Poll updated successfully" });
            }
            ViewBag.PollId = id;
            return View(model);
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Remove(int id)
        {
            await _pollService.Remove(id);
            await _auditService.AddLog(AuditLogType.delete_poll, int.Parse(HttpContext.User.Identity.Name), null, null, 0, id);
            return RedirectToAction("Index", "Polls", new { message = "Poll removed" });
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Toggle(int id)
        {
            await _pollService.ToggleEnabled(id);
            return RedirectToAction("Index", "Polls", new { message = "Poll status toggled" });
        }

        [HousekeepingFilter(Fuse.fuse_administrator_access)]
        public async Task<IActionResult> Results(int id)
        {
            var results = await _pollService.GetResults(id);
            if (results == null) return RedirectToAction("Index", new { message = "Poll not found" });
            return View(results);
        }
    }
}
