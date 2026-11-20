using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// Hobba tools > Hobba tracking tools, after Sulake's: "Banned Habbos trying to enter" (Odin v1.34.0 logs every login of a
    /// banned Habbo and every connection from a banned IP address in banned_login_attempts) and "Calls for help" statistics
    /// (calls_for_help: per staff member, per hour of the day, per day).
    /// </summary>
    [Area("Housekeeping")]
    public class TrackingController : Controller
    {
        public static readonly int[] Periods = { 1, 7, 30, 90 };

        private readonly DataContext _context;

        public TrackingController(DataContext context)
        {
            _context = context;
        }

        public record AttemptRow(DateTime At, int? UserId, string Username, string Ip, string BanType, int BanId, string Reason, long? BannedUntil);

        [HousekeepingFilter(Fuse.fuse_ban)]
        public async Task<IActionResult> Banned(int days = 7)
        {
            if (!Periods.Contains(days)) days = 7;
            var sql = new ContextSql(_context);
            var rows = await sql.QueryOpen(
                "SELECT a.attempted_at, a.user_id, a.username, a.ip, a.ban_type, a.ban_id, b.message, b.banned_until FROM banned_login_attempts a " +
                "LEFT JOIN users_bans b ON b.id = a.ban_id WHERE a.attempted_at > NOW() - INTERVAL @days DAY ORDER BY a.id DESC LIMIT 500",
                r => new AttemptRow(r.GetDateTime(0), r.IsDBNull(1) ? null : r.GetInt32(1), r.IsDBNull(2) ? null : r.GetString(2), r.GetString(3), r.GetString(4),
                    r.GetInt32(5), r.IsDBNull(6) ? null : r.GetString(6), r.IsDBNull(7) ? null : r.GetInt64(7)), ("@days", days));
            ViewBag.Days = days;
            return View(rows);
        }

        public record StaffRow(int StaffId, string Staff, int Picked, int Replied, double? AvgMinutes);

        public class CallsPage
        {
            public int Days { get; set; }
            public int Total { get; set; }
            public int NeverPicked { get; set; }
            public double? AvgMinutes { get; set; }
            public List<StaffRow> Staff { get; set; }
            public int[] ByHour { get; set; } = new int[24];
            public List<(DateTime Day, int Calls, int Picked)> ByDay { get; set; }
        }

        [HousekeepingFilter(Fuse.fuse_receive_calls_for_help)]
        public async Task<IActionResult> Calls(int days = 30)
        {
            if (!Periods.Contains(days)) days = 30;
            var since = DateTime.Now.AddDays(-days);
            var sql = new ContextSql(_context);
            var calls = await sql.QueryOpen("SELECT created_at, picked_up_by, picked_up_username, picked_up_at, closed_reason FROM calls_for_help WHERE created_at > @since",
                r => (Created: r.GetDateTime(0), StaffId: r.IsDBNull(1) ? 0 : r.GetInt32(1), Staff: r.IsDBNull(2) ? null : r.GetString(2),
                    PickedAt: r.IsDBNull(3) ? (DateTime?)null : r.GetDateTime(3), Reason: r.IsDBNull(4) ? null : r.GetString(4)), ("@since", since));

            double? Avg(IEnumerable<(DateTime Created, int StaffId, string Staff, DateTime? PickedAt, string Reason)> list)
            {
                var minutes = list.Where(c => c.PickedAt != null && c.PickedAt >= c.Created).Select(c => (c.PickedAt.Value - c.Created).TotalMinutes).ToList();
                return minutes.Count == 0 ? null : minutes.Average();
            }

            var page = new CallsPage
            {
                Days = days,
                Total = calls.Count,
                NeverPicked = calls.Count(c => c.StaffId == 0),
                AvgMinutes = Avg(calls),
                Staff = calls.Where(c => c.StaffId > 0).GroupBy(c => c.StaffId)
                    .Select(g => new StaffRow(g.Key, g.Select(c => c.Staff).FirstOrDefault(s => s != null), g.Count(), g.Count(c => c.Reason == "replied"), Avg(g)))
                    .OrderByDescending(s => s.Picked).ToList(),
                ByDay = calls.GroupBy(c => c.Created.Date).OrderByDescending(g => g.Key).Select(g => (g.Key, g.Count(), g.Count(c => c.StaffId > 0))).ToList(),
            };
            foreach (var c in calls) page.ByHour[c.Created.Hour]++;
            return View(page);
        }
    }
}
