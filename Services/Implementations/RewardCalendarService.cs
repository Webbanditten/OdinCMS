using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services.Implementations
{
    /// <summary>
    /// Housekeeping's reward calendars: furni pools, saved plans (templates), generating a calendar from a plan,
    /// copying a period and the month view. Odin hands out the resulting `rewards` rows at login.
    /// </summary>
    public class RewardCalendarService : IRewardCalendarService
    {
        private static readonly JsonSerializerOptions PlanJson = new(JsonSerializerDefaults.Web);
        private readonly DataContext _context;

        public RewardCalendarService(DataContext context)
        {
            _context = context;
        }

        // ---- Furni ----

        private async Task<Dictionary<int, RewardFurni>> Furni(IEnumerable<int> ids)
        {
            var wanted = ids.Distinct().ToList();
            return await _context.ItemsDefinitions
                .Where(d => wanted.Contains(d.Id))
                .Select(d => new RewardFurni { Id = d.Id, Name = d.Name, Sprite = d.Sprite })
                .ToDictionaryAsync(d => d.Id);
        }

        /// <summary>
        /// Gift notes with line breaks as the game wants them: Odin's filterInput turns "\n" into a space and keeps "\r",
        /// which the client shows as a new line ("\r\n" from earlier rewards gave each new line a leading space).
        /// </summary>
        public static string GameNote(string note) =>
            (note ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Trim().Replace("\n", "\r");

        private static List<int> ParseItems(string itemDefinitions) =>
            (itemDefinitions ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

        // ---- Pools ----

        public async Task<List<RewardPoolView>> GetPools()
        {
            var pools = await _context.RewardPools.Include(p => p.Items).OrderBy(p => p.Name).ToListAsync();
            var furni = await Furni(pools.SelectMany(p => p.Items.Select(i => i.DefinitionId)));
            return pools.Select(p => new RewardPoolView
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Items = p.Items.OrderBy(i => i.SortOrder).ThenBy(i => i.DefinitionId)
                    .Where(i => furni.ContainsKey(i.DefinitionId))
                    .Select(i => new RewardPoolFurni
                    {
                        Id = i.DefinitionId,
                        Name = furni[i.DefinitionId].Name,
                        Sprite = furni[i.DefinitionId].Sprite,
                        NeedsReview = i.NeedsReview,
                        Note = i.Note,
                    }).ToList(),
            }).ToList();
        }

        public async Task<RewardPool> CreatePool(string name, string description)
        {
            var pool = new RewardPool { Name = name.Trim(), Description = description?.Trim() ?? "" };
            _context.RewardPools.Add(pool);
            await _context.SaveChangesAsync();
            return pool;
        }

        public async Task UpdatePool(int id, string name, string description)
        {
            await _context.RewardPools.Where(p => p.Id == id).ExecuteUpdateAsync(set => set
                .SetProperty(p => p.Name, name.Trim())
                .SetProperty(p => p.Description, description == null ? "" : description.Trim()));
        }

        public async Task DeletePool(int id)
        {
            // Not ExecuteDeleteAsync: the MySQL provider writes "DELETE FROM t AS a", which MariaDB 11.4 rejects.
            _context.RewardPools.RemoveRange(await _context.RewardPools.Where(p => p.Id == id).ToListAsync());
            await _context.SaveChangesAsync();
        }

        public async Task AddPoolItems(int poolId, IEnumerable<int> definitionIds)
        {
            var existing = await _context.RewardPoolItems.Where(i => i.PoolId == poolId).ToListAsync();
            var known = (await Furni(definitionIds)).Keys;
            var order = existing.Count == 0 ? 0 : existing.Max(i => i.SortOrder) + 1;
            foreach (var id in definitionIds.Distinct().Where(id => known.Contains(id) && existing.All(e => e.DefinitionId != id)))
            {
                _context.RewardPoolItems.Add(new RewardPoolItem { PoolId = poolId, DefinitionId = id, SortOrder = order++ });
            }
            await _context.SaveChangesAsync();
        }

        public async Task RemovePoolItem(int poolId, int definitionId)
        {
            _context.RewardPoolItems.RemoveRange(await _context.RewardPoolItems.Where(i => i.PoolId == poolId && i.DefinitionId == definitionId).ToListAsync());
            await _context.SaveChangesAsync();
        }

        public async Task ConfirmPoolItem(int poolId, int definitionId)
        {
            await _context.RewardPoolItems.Where(i => i.PoolId == poolId && i.DefinitionId == definitionId)
                .ExecuteUpdateAsync(set => set.SetProperty(i => i.NeedsReview, false));
        }

        /// <summary>Move a furni one place up (-1) or down (+1) in the pool's order (for "in order" calendars).</summary>
        public async Task MovePoolItem(int poolId, int definitionId, int direction)
        {
            var items = await _context.RewardPoolItems.Where(i => i.PoolId == poolId)
                .OrderBy(i => i.SortOrder).ThenBy(i => i.DefinitionId).ToListAsync();
            var index = items.FindIndex(i => i.DefinitionId == definitionId);
            var other = index + Math.Sign(direction);
            if (index < 0 || other < 0 || other >= items.Count) return;

            (items[index], items[other]) = (items[other], items[index]);
            for (var i = 0; i < items.Count; i++) items[i].SortOrder = i;
            await _context.SaveChangesAsync();
        }

        // ---- Templates ----

        public async Task<List<RewardTemplate>> GetTemplates() =>
            await _context.RewardTemplates.OrderBy(t => t.Name).ToListAsync();

        public RewardPlan ReadPlan(RewardTemplate template) =>
            JsonSerializer.Deserialize<RewardPlan>(template.Plan, PlanJson) ?? new RewardPlan();

        public async Task<RewardTemplate> SaveTemplate(string name, RewardPlan plan)
        {
            name = name.Trim();
            var template = await _context.RewardTemplates.FirstOrDefaultAsync(t => t.Name == name);
            if (template == null)
            {
                template = new RewardTemplate { Name = name };
                _context.RewardTemplates.Add(template);
            }
            template.Plan = JsonSerializer.Serialize(plan, PlanJson);
            template.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task DeleteTemplate(int id)
        {
            _context.RewardTemplates.RemoveRange(await _context.RewardTemplates.Where(t => t.Id == id).ToListAsync());
            await _context.SaveChangesAsync();
        }

        // ---- Generating a calendar ----

        private static bool Applies(RewardPlanSlot slot, int index, int days, DateTime date)
        {
            var isoWeekday = ((int)date.DayOfWeek + 6) % 7 + 1; // 1 = Monday ... 7 = Sunday
            return slot.When switch
            {
                RewardPlanSlot.WhenWeekdays => slot.Weekdays.Contains(isoWeekday),
                RewardPlanSlot.WhenDays => slot.Days.Contains(index + 1),
                RewardPlanSlot.WhenEveryN => slot.EveryN > 0 && index % slot.EveryN == 0,
                RewardPlanSlot.WhenLastDay => index == days - 1,
                _ => true,
            };
        }

        /// <summary>
        /// The calendar a plan gives from the start date: one entry per day, in order. Random picks differ between
        /// calls; in order / cycle picks follow the pool's order (or the fixed list's).
        /// </summary>
        public async Task<List<RewardPlanDay>> Generate(RewardPlan plan, DateTime start)
        {
            var days = Math.Clamp(plan.Days, 1, RewardPlan.MaxDays);
            var calendar = Enumerable.Range(0, days).Select(i => new RewardPlanDay { Date = start.Date.AddDays(i) }).ToList();

            var poolIds = plan.Slots.Where(s => s.PoolId != null).Select(s => s.PoolId.Value).Distinct().ToList();
            var pools = await _context.RewardPoolItems.Where(i => poolIds.Contains(i.PoolId))
                .OrderBy(i => i.SortOrder).ThenBy(i => i.DefinitionId).ToListAsync();
            var known = (await Furni(pools.Select(i => i.DefinitionId).Concat(plan.Slots.SelectMany(s => s.Items)))).Keys.ToHashSet();

            foreach (var slot in plan.Slots)
            {
                var source = (slot.PoolId != null
                        ? pools.Where(i => i.PoolId == slot.PoolId).Select(i => i.DefinitionId)
                        : slot.Items)
                    .Where(known.Contains).ToList();
                if (source.Count == 0) continue;

                var next = 0;
                var bag = new List<int>();
                for (var index = 0; index < days; index++)
                {
                    if (!Applies(slot, index, days, calendar[index].Date)) continue;
                    var day = calendar[index];
                    for (var n = 0; n < Math.Clamp(slot.Count, 1, 10); n++)
                    {
                        int? pick;
                        switch (slot.Pick)
                        {
                            case RewardPlanSlot.PickInOrder:
                                pick = next < source.Count ? source[next++] : null;
                                break;
                            case RewardPlanSlot.PickCycle:
                                pick = source[next++ % source.Count];
                                break;
                            default:
                                if (bag.Count == 0) bag = source.OrderBy(_ => Random.Shared.Next()).ToList();
                                // Prefer a furni this day doesn't have yet.
                                var choice = bag.FirstOrDefault(id => !day.Items.Contains(id), bag[0]);
                                bag.Remove(choice);
                                pick = choice;
                                break;
                        }
                        if (pick != null) day.Items.Add(pick.Value);
                    }
                }
            }
            return calendar;
        }

        /// <summary>Create the calendar's rewards: each day from midnight to midnight, with the plan's note and label.</summary>
        public async Task<int> CreateCalendar(IEnumerable<RewardPlanDay> days, string note, string label)
        {
            var created = 0;
            foreach (var day in days.Where(d => d.Items.Count > 0 || !string.IsNullOrWhiteSpace(d.Badge)))
            {
                _context.Rewards.Add(new Rewards
                {
                    AvailableFrom = day.Date.Date,
                    AvailableTo = day.Date.Date.AddDays(1),
                    ItemDefinitions = string.Join(",", day.Items),
                    Badge = string.IsNullOrWhiteSpace(day.Badge) ? null : day.Badge.Trim().ToUpperInvariant(),
                    Description = GameNote(note),
                    Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim(),
                });
                created++;
            }
            await _context.SaveChangesAsync();
            return created;
        }

        /// <summary>
        /// Copy the rewards that start between from and to (whole days) so the first day lands on target: same furni,
        /// badge, note and times. The label is replaced when one is given.
        /// </summary>
        public async Task<int> CopyPeriod(DateTime from, DateTime to, DateTime target, string label)
        {
            var source = await _context.Rewards
                .Where(r => r.AvailableFrom >= from.Date && r.AvailableFrom < to.Date.AddDays(1))
                .OrderBy(r => r.AvailableFrom).ToListAsync();
            var shift = target.Date - from.Date;
            foreach (var reward in source)
            {
                _context.Rewards.Add(new Rewards
                {
                    AvailableFrom = reward.AvailableFrom + shift,
                    AvailableTo = reward.AvailableTo + shift,
                    ItemDefinitions = reward.ItemDefinitions,
                    Badge = reward.Badge,
                    Description = reward.Description,
                    RequiredStreak = reward.RequiredStreak,
                    Label = string.IsNullOrWhiteSpace(label) ? reward.Label : label.Trim(),
                });
            }
            await _context.SaveChangesAsync();
            return source.Count;
        }

        // ---- The month view and single rewards ----

        /// <summary>Rewards that start between from (inclusive) and to (exclusive), with furni and how many players got them.</summary>
        public async Task<List<RewardEntry>> GetRewards(DateTime from, DateTime to)
        {
            var rewards = await _context.Rewards
                .Where(r => r.AvailableFrom >= from && r.AvailableFrom < to)
                .OrderBy(r => r.AvailableFrom).ToListAsync();
            var ids = rewards.Select(r => r.Id).ToList();
            var redeemed = await _context.RewardsRedeemed.Where(r => ids.Contains(r.RewardId))
                .GroupBy(r => r.RewardId).Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);
            var furni = await Furni(rewards.SelectMany(r => ParseItems(r.ItemDefinitions)));

            return rewards.Select(r => new RewardEntry
            {
                Id = r.Id,
                AvailableFrom = r.AvailableFrom,
                AvailableTo = r.AvailableTo,
                Description = r.Description,
                Badge = r.Badge,
                Label = r.Label,
                Redeemed = redeemed.GetValueOrDefault(r.Id),
                Items = ParseItems(r.ItemDefinitions).Where(furni.ContainsKey).Select(id => furni[id]).ToList(),
            }).ToList();
        }

        public async Task<List<RewardFurni>> GetFurni(IEnumerable<int> ids)
        {
            var furni = await Furni(ids);
            return ids.Where(furni.ContainsKey).Select(id => furni[id]).ToList();
        }

        public async Task<Rewards> SaveReward(int id, DateTime from, DateTime to, IEnumerable<int> items, string badge, string note, string label)
        {
            var reward = id > 0 ? await _context.Rewards.FirstOrDefaultAsync(r => r.Id == id) : null;
            if (reward == null)
            {
                reward = new Rewards();
                _context.Rewards.Add(reward);
            }
            reward.AvailableFrom = from;
            reward.AvailableTo = to;
            reward.ItemDefinitions = string.Join(",", items);
            reward.Badge = string.IsNullOrWhiteSpace(badge) ? null : badge.Trim().ToUpperInvariant();
            reward.Description = GameNote(note);
            reward.Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
            await _context.SaveChangesAsync();
            return reward;
        }

        public async Task DeleteReward(int id)
        {
            _context.Rewards.RemoveRange(await _context.Rewards.Where(r => r.Id == id).ToListAsync());
            await _context.SaveChangesAsync();
        }

        /// <summary>Delete a calendar's rewards that haven't started yet (started ones keep who got them).</summary>
        public async Task<int> DeleteUpcoming(string label)
        {
            var now = DateTime.Now;
            var upcoming = await _context.Rewards.Where(r => r.Label == label && r.AvailableFrom > now).ToListAsync();
            _context.Rewards.RemoveRange(upcoming);
            await _context.SaveChangesAsync();
            return upcoming.Count;
        }

        public async Task<List<string>> GetLabels() =>
            await _context.Rewards.Where(r => r.Label != null).Select(r => r.Label).Distinct().OrderBy(l => l).ToListAsync();
    }
}
