using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// Reward management: the month view (Index), the calendar builder (Builder) and furni pools (Pools).
    /// The pages talk JSON to the actions below (wwwroot/housekeeping/js/rewards.js).
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.housekeeping_rewards)]
    public class RewardsController : Controller
    {
        private readonly IRewardCalendarService _calendar;

        public RewardsController(IRewardCalendarService calendar)
        {
            _calendar = calendar;
        }

        // ---- Pages ----

        public IActionResult Index() => View();

        public IActionResult Builder() => View();

        public IActionResult Pools() => View();

        // ---- Month view ----

        /// <summary>Rewards starting in the six weeks the month grid shows (from the Monday before the 1st).</summary>
        [HttpGet]
        public async Task<IActionResult> Month(int year, int month)
        {
            var first = new DateTime(year, month, 1);
            var from = first.AddDays(-(((int)first.DayOfWeek + 6) % 7));
            return Json(new
            {
                rewards = await _calendar.GetRewards(from, from.AddDays(42)),
                labels = await _calendar.GetLabels(),
            });
        }

        public class SaveRewardRequest
        {
            public int Id { get; set; }
            public DateTime AvailableFrom { get; set; }
            public DateTime AvailableTo { get; set; }
            public List<int> Items { get; set; } = new();
            public string Badge { get; set; }
            public string Note { get; set; }
            public string Label { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveRewardRequest request)
        {
            if (request == null) return BadRequest("Missing reward.");
            if (request.AvailableTo <= request.AvailableFrom) return BadRequest("The reward must end after it starts.");
            if (request.Items.Count == 0 && string.IsNullOrWhiteSpace(request.Badge)) return BadRequest("Give at least one furni or a badge.");
            if (!string.IsNullOrWhiteSpace(request.Badge) && request.Badge.Trim().Length != 3) return BadRequest("Badge codes are 3 characters.");
            if (string.IsNullOrWhiteSpace(request.Note)) return BadRequest("Write a gift note.");

            var reward = await _calendar.SaveReward(request.Id, request.AvailableFrom, request.AvailableTo, request.Items, request.Badge, request.Note, request.Label);
            return Json(new { id = reward.Id });
        }

        public class IdRequest
        {
            public int Id { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] IdRequest request)
        {
            await _calendar.DeleteReward(request.Id);
            return Ok();
        }

        public class LabelRequest
        {
            public string Label { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUpcoming([FromBody] LabelRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Label)) return BadRequest("Pick a calendar.");
            return Json(new { deleted = await _calendar.DeleteUpcoming(request.Label) });
        }

        public class CopyRequest
        {
            public DateTime From { get; set; }
            public DateTime To { get; set; }
            public DateTime Target { get; set; }
            public string Label { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CopyPeriod([FromBody] CopyRequest request)
        {
            if (request == null || request.To < request.From) return BadRequest("The period ends before it starts.");
            return Json(new { copied = await _calendar.CopyPeriod(request.From, request.To, request.Target, request.Label) });
        }

        // ---- Calendar builder ----

        public class PreviewRequest
        {
            public RewardPlan Plan { get; set; }
            public DateTime Start { get; set; }
        }

        /// <summary>The calendar a plan gives, with the furni to show and the days that already have rewards.</summary>
        [HttpPost]
        public async Task<IActionResult> Preview([FromBody] PreviewRequest request)
        {
            if (request?.Plan == null) return BadRequest("Missing plan.");
            var days = await _calendar.Generate(request.Plan, request.Start);
            var furni = (await _calendar.GetFurni(days.SelectMany(d => d.Items).Distinct())).ToDictionary(f => f.Id);
            var existing = await _calendar.GetRewards(days.First().Date, days.Last().Date.AddDays(1));
            return Json(new
            {
                days = days.Select(d => new { date = d.Date.ToString("yyyy-MM-dd"), items = d.Items.Where(furni.ContainsKey).Select(id => furni[id]), badge = d.Badge }),
                existing = existing.GroupBy(r => r.AvailableFrom.ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => g.Select(r => r.Label ?? r.Description).ToList()),
            });
        }

        public class CreateCalendarRequest
        {
            public List<RewardPlanDay> Days { get; set; } = new();
            public string Note { get; set; }
            public string Label { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCalendar([FromBody] CreateCalendarRequest request)
        {
            if (request == null || request.Days.Count == 0) return BadRequest("The calendar has no days.");
            if (string.IsNullOrWhiteSpace(request.Note)) return BadRequest("Write a gift note.");
            if (request.Days.Any(d => !string.IsNullOrWhiteSpace(d.Badge) && d.Badge.Trim().Length != 3)) return BadRequest("Badge codes are 3 characters.");
            return Json(new { created = await _calendar.CreateCalendar(request.Days, request.Note, request.Label) });
        }

        [HttpGet]
        public async Task<IActionResult> Templates()
        {
            var templates = await _calendar.GetTemplates();
            return Json(templates.Select(t => new { id = t.Id, name = t.Name, plan = _calendar.ReadPlan(t) }));
        }

        public class SaveTemplateRequest
        {
            public string Name { get; set; }
            public RewardPlan Plan { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SaveTemplate([FromBody] SaveTemplateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name) || request.Plan == null) return BadRequest("Give the template a name.");
            var template = await _calendar.SaveTemplate(request.Name, request.Plan);
            return Json(new { id = template.Id });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTemplate([FromBody] IdRequest request)
        {
            await _calendar.DeleteTemplate(request.Id);
            return Ok();
        }

        // ---- Pools ----

        [HttpGet]
        public async Task<IActionResult> PoolList() => Json(await _calendar.GetPools());

        public class PoolRequest
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SavePool([FromBody] PoolRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name)) return BadRequest("Give the pool a name.");
            if ((await _calendar.GetPools()).Any(p => p.Id != request.Id && string.Equals(p.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest("There is already a pool with that name.");
            }
            if (request.Id > 0)
            {
                await _calendar.UpdatePool(request.Id, request.Name, request.Description);
                return Json(new { id = request.Id });
            }
            return Json(new { id = (await _calendar.CreatePool(request.Name, request.Description)).Id });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePool([FromBody] IdRequest request)
        {
            await _calendar.DeletePool(request.Id);
            return Ok();
        }

        public class PoolItemRequest
        {
            public int PoolId { get; set; }
            public List<int> Items { get; set; } = new();
            public int Item { get; set; }
            public int Direction { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> AddPoolItems([FromBody] PoolItemRequest request)
        {
            await _calendar.AddPoolItems(request.PoolId, request.Items);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RemovePoolItem([FromBody] PoolItemRequest request)
        {
            await _calendar.RemovePoolItem(request.PoolId, request.Item);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPoolItem([FromBody] PoolItemRequest request)
        {
            await _calendar.ConfirmPoolItem(request.PoolId, request.Item);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MovePoolItem([FromBody] PoolItemRequest request)
        {
            await _calendar.MovePoolItem(request.PoolId, request.Item, request.Direction);
            return Ok();
        }
    }
}
