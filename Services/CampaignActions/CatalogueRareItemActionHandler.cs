using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KeplerCMS.Data;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services.CampaignActions
{
    /// <summary>
    /// Campaign action that controls the catalogue "rare item" (the single
    /// catalogue_items row with sale_code = 'rare_item') and the visibility of
    /// its page. While the campaign is active the page fuse is DEFAULT (visible
    /// to all) and the rare item sells the chosen definition. When the campaign
    /// ends, the page fuse is set to fuse_catalogue_administrator (hidden); the
    /// last-set definition is intentionally left in place.
    /// </summary>
    public class CatalogueRareItemActionHandler : ICampaignActionHandler
    {
        private readonly DataContext _context;
        private const string RareSaleCode = "rare_item";
        private const string ActiveFuse = "DEFAULT";
        private const string HiddenFuse = "fuse_catalogue_administrator";
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public CatalogueRareItemActionHandler(DataContext context)
        {
            _context = context;
        }

        public Task<string> TakeSnapshot(string actionData)
        {
            // The definition_id is intentionally kept on deactivate (only the page
            // is hidden), so there is no prior state to capture.
            return Task.FromResult("{}");
        }

        public async Task Apply(string actionData)
        {
            var data = JsonSerializer.Deserialize<CatalogueRareItemActionData>(actionData, _jsonOptions);

            // Update the rare item's definition via raw SQL. The CatalogueItems entity
            // maps a non-existent "sprite" column, so materializing it would fail;
            // a raw UPDATE avoids that.
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE catalogue_items SET definition_id = {0} WHERE sale_code = {1}",
                data.DefinitionId, RareSaleCode);

            await SetRarePageFuse(ActiveFuse);
        }

        public async Task Revert(string snapshotData)
        {
            // Hide the rare page again; leave the last-set definition_id untouched.
            await SetRarePageFuse(HiddenFuse);
        }

        public string GetConflictKey(string actionData)
        {
            return "catalogue_rare_item";
        }

        private async Task SetRarePageFuse(string fuse)
        {
            // Read only the page id (projection) to avoid materializing CatalogueItems.
            var pageId = await _context.CatalogueItems
                .Where(c => c.SaleCode == RareSaleCode)
                .Select(c => c.PageId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(pageId) || !int.TryParse(pageId, out var pageIdInt))
                return;

            var page = await _context.CataloguePages.FindAsync(pageIdInt);
            if (page != null)
            {
                page.Fuse = fuse;
                await _context.SaveChangesAsync();
            }
        }
    }

    public class CatalogueRareItemActionData
    {
        public int DefinitionId { get; set; }
        // Display text from the furni picker ("(sprite) name"), used for the
        // action summary and for re-populating the picker when editing.
        public string Label { get; set; }
    }
}
