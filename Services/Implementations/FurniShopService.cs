using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services.Implementations
{
    public class FurniShopService : IFurniShopService
    {
        private readonly DataContext _context;
        private readonly ContextSql _sql;
        private readonly ICatalogueFurniService _catalogue;
        private readonly ICreditService _credits;
        private readonly ICommandQueueService _commandQueue;

        public FurniShopService(DataContext context, ICatalogueFurniService catalogue, ICreditService credits, ICommandQueueService commandQueue)
        {
            _context = context;
            _sql = new ContextSql(context);
            _catalogue = catalogue;
            _credits = credits;
            _commandQueue = commandQueue;
        }

        public async Task<CatalogueFurniEntry> Get(int catalogueItemId) =>
            (await _catalogue.Load()).ById.GetValueOrDefault(catalogueItemId);

        public string WhyNotForSale(CatalogueFurniEntry entry)
        {
            if (entry == null) return "No catalogue item has this id";
            if (entry.IsHidden) return "Hidden in the catalogue";
            if (!entry.IsPublic) return "Only on catalogue pages players can't open (staff, Habbo Club or switched-off pages)";
            return entry.NotGivable;
        }

        public async Task<List<CatalogueFurniEntry>> Search(string query)
        {
            query = (query ?? "").Trim();
            if (query.Length < 2) return new List<CatalogueFurniEntry>();
            var isId = int.TryParse(query, out var id);
            // Exact name, sale code or id first, then names starting with the query, then the rest; buyable before not.
            int Rank(CatalogueFurniEntry e) =>
                (isId && e.Id == id) || string.Equals(e.Name, query, StringComparison.OrdinalIgnoreCase) || string.Equals(e.SaleCode, query, StringComparison.OrdinalIgnoreCase) ? 0
                : (e.Name ?? "").StartsWith(query, StringComparison.OrdinalIgnoreCase) || e.SaleCode.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 1 : 2;
            return (await _catalogue.Load()).ById.Values
                .Where(e => (isId && e.Id == id) || e.SaleCode.Contains(query, StringComparison.OrdinalIgnoreCase) || (e.Name ?? "").Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(Rank).ThenBy(e => WhyNotForSale(e) != null).ThenBy(e => e.Name).ThenBy(e => e.Id)
                .Take(80)
                .ToList();
        }

        public async Task<FurniPurchaseOutcome> Purchase(int catalogueItemId, int userId)
        {
            var entry = await Get(catalogueItemId);
            if (entry == null) return FurniPurchaseOutcome.NotFound;
            if (WhyNotForSale(entry) != null) return FurniPurchaseOutcome.NotForSale;

            var outcome = await _sql.InConnection(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var username = (await _sql.Query("SELECT username FROM users WHERE id = @user", r => r.GetString(0), ("@user", userId))).FirstOrDefault();
                if (username == null) return FurniPurchaseOutcome.NotFound;

                // One conditional UPDATE, so parallel clicks can't spend the same credits twice.
                if (entry.Price > 0)
                {
                    var paid = await _sql.Execute("UPDATE users SET credits = credits - @price WHERE id = @user AND credits >= @price", ("@price", entry.Price), ("@user", userId));
                    if (paid == 0) return FurniPurchaseOutcome.NotEnoughCredits;
                }

                var film = 0;
                foreach (var part in entry.Parts)
                {
                    for (var i = 0; i < part.Amount; i++) film += await _catalogue.Give(userId, username, part);
                }
                if (film > 0) await _sql.Execute("UPDATE users SET film = film + @film WHERE id = @user", ("@film", film), ("@user", userId));

                await transaction.CommitAsync();
                return FurniPurchaseOutcome.Bought;
            });
            if (outcome != FurniPurchaseOutcome.Bought) return outcome;

            await _credits.LogPurchase(userId, PurchaseLog.WebFurni, entry.Name, entry.Price,
                entry.IsPackage ? null : entry.FirstDefinition?.Id, entry.Id, "DEFAULT");
            // Odin keeps an online player's credits and hand in memory.
            if (entry.Price > 0)
                _commandQueue.QueueCommand(CommandQueueType.reduce_credits, new CommandTemplate { UserId = userId, Credits = entry.Price });
            _commandQueue.QueueCommand(CommandQueueType.reload_inventory, new CommandTemplate { UserId = userId });
            return FurniPurchaseOutcome.Bought;
        }
    }
}
