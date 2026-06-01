using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KeplerCMS.Data;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services.CampaignActions
{
    public class CatalogueVisibilityActionHandler : ICampaignActionHandler
    {
        private readonly DataContext _context;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public CatalogueVisibilityActionHandler(DataContext context)
        {
            _context = context;
        }

        public async Task<string> TakeSnapshot(string actionData)
        {
            var data = JsonSerializer.Deserialize<CatalogueVisibilityActionData>(actionData, _jsonOptions);
            var pageIds = data.Pages.Select(p => p.PageId).ToList();
            
            var currentStates = await _context.CataloguePages
                .Where(p => pageIds.Contains(p.Id))
                .Select(p => new CataloguePageSnapshot
                {
                    PageId = p.Id,
                    IndexVisible = p.IndexVisible,
                    ClubOnly = p.ClubOnly,
                    Fuse = p.Fuse,
                    OrderId = p.OrderId
                })
                .ToListAsync();

            return JsonSerializer.Serialize(new CatalogueVisibilitySnapshotData { Pages = currentStates });
        }

        public async Task Apply(string actionData)
        {
            var data = JsonSerializer.Deserialize<CatalogueVisibilityActionData>(actionData, _jsonOptions);
            
            foreach (var pageChange in data.Pages)
            {
                var page = await _context.CataloguePages.FindAsync(pageChange.PageId);
                if (page != null)
                {
                    if (pageChange.IndexVisible.HasValue)
                        page.IndexVisible = pageChange.IndexVisible.Value;
                    if (pageChange.ClubOnly.HasValue)
                        page.ClubOnly = pageChange.ClubOnly.Value;
                    if (pageChange.Fuse != null)
                        page.Fuse = pageChange.Fuse;
                    if (pageChange.OrderId.HasValue)
                        page.OrderId = pageChange.OrderId.Value;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task Revert(string snapshotData)
        {
            var data = JsonSerializer.Deserialize<CatalogueVisibilitySnapshotData>(snapshotData, _jsonOptions);

            foreach (var pageState in data.Pages)
            {
                var page = await _context.CataloguePages.FindAsync(pageState.PageId);
                if (page != null)
                {
                    page.IndexVisible = pageState.IndexVisible;
                    page.ClubOnly = pageState.ClubOnly;
                    page.Fuse = pageState.Fuse;
                    page.OrderId = pageState.OrderId;
                }
            }

            await _context.SaveChangesAsync();
        }

        public string GetConflictKey(string actionData)
        {
            var data = JsonSerializer.Deserialize<CatalogueVisibilityActionData>(actionData, _jsonOptions);
            var pageIds = string.Join(",", data.Pages.Select(p => p.PageId).OrderBy(id => id));
            return $"catalogue_visibility:{pageIds}";
        }
    }

    public class CatalogueVisibilityActionData
    {
        public List<CataloguePageChange> Pages { get; set; } = new List<CataloguePageChange>();
    }

    /// <summary>
    /// Defines desired state for a catalogue page when campaign is active.
    /// Null values mean "don't change this field".
    /// </summary>
    public class CataloguePageChange
    {
        public int PageId { get; set; }
        public int? IndexVisible { get; set; }
        public int? ClubOnly { get; set; }
        public string Fuse { get; set; }
        public int? OrderId { get; set; }
    }

    public class CatalogueVisibilitySnapshotData
    {
        public List<CataloguePageSnapshot> Pages { get; set; } = new List<CataloguePageSnapshot>();
    }

    /// <summary>
    /// Stores the original state of a catalogue page before campaign modification.
    /// </summary>
    public class CataloguePageSnapshot
    {
        public int PageId { get; set; }
        public int IndexVisible { get; set; }
        public int ClubOnly { get; set; }
        public string Fuse { get; set; }
        public int OrderId { get; set; }
    }
}
