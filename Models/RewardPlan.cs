using System;
using System.Collections.Generic;

namespace KeplerCMS.Models
{
    /// <summary>
    /// A reward calendar: how many days, the gift note, and slots that say what each day gets. The builder posts it
    /// for a preview; templates (cms_reward_templates.plan) store it as JSON with camelCase names.
    /// </summary>
    public class RewardPlan
    {
        public const int MaxDays = 62;

        public int Days { get; set; } = 24;
        public string Note { get; set; } = "";
        /// <summary>Names the calendar on its rewards ("Julekalender 2026").</summary>
        public string Label { get; set; } = "";
        public List<RewardPlanSlot> Slots { get; set; } = new();
    }

    /// <summary>One line of the plan: which furni (a pool or a fixed list), how to pick, and on which days.</summary>
    public class RewardPlanSlot
    {
        public const string PickRandom = "random";       // a different furni each time until the source runs out, then reshuffle
        public const string PickInOrder = "in_order";    // the source in its order, one per day; stops when it runs out
        public const string PickCycle = "cycle";         // the source in its order, starting over at the end

        public const string WhenEveryDay = "every_day";
        public const string WhenWeekdays = "weekdays";   // Weekdays: 1 = Monday ... 7 = Sunday
        public const string WhenDays = "days";           // Days: day numbers in the calendar, 1 = first day
        public const string WhenEveryN = "every_n";      // day 1, 1 + N, 1 + 2N ...
        public const string WhenLastDay = "last_day";

        /// <summary>Pick from this pool; null means from Items.</summary>
        public int? PoolId { get; set; }
        public List<int> Items { get; set; } = new();
        public string Pick { get; set; } = PickRandom;
        public string When { get; set; } = WhenEveryDay;
        public List<int> Weekdays { get; set; } = new();
        public List<int> Days { get; set; } = new();
        public int EveryN { get; set; } = 7;
        /// <summary>Furni per day from this slot.</summary>
        public int Count { get; set; } = 1;
    }

    /// <summary>One day of a calendar, as previewed and as posted back to create it.</summary>
    public class RewardPlanDay
    {
        public DateTime Date { get; set; }
        public List<int> Items { get; set; } = new();
        public string Badge { get; set; }
    }

    /// <summary>A furni as the reward pages show it.</summary>
    public class RewardFurni
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Sprite { get; set; }
        /// <summary>The sprite for the furni imager ("poster27"; otherwise Sprite).</summary>
        public string Image { get; set; }
    }

    /// <summary>A reward as the month view shows it.</summary>
    public class RewardEntry
    {
        public int Id { get; set; }
        public DateTime AvailableFrom { get; set; }
        public DateTime AvailableTo { get; set; }
        public string Description { get; set; }
        public string Badge { get; set; }
        public string Label { get; set; }
        public List<RewardFurni> Items { get; set; } = new();
        /// <summary>How many players got it (deleting the reward deletes these records too).</summary>
        public int Redeemed { get; set; }
    }

    /// <summary>A pool with its furni, for the pool page and the builder.</summary>
    public class RewardPoolView
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<RewardPoolFurni> Items { get; set; } = new();
    }

    public class RewardPoolFurni : RewardFurni
    {
        public bool NeedsReview { get; set; }
        public string Note { get; set; }
    }
}
