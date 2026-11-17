using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class CreditService : ICreditService
    {
        private readonly ICommandQueueService _commandQueueService;
        private readonly DataContext _context;

        public CreditService(ICommandQueueService commandQueueService, DataContext context)
        {
            _commandQueueService = commandQueueService;
            _context = context;
        }

        /// <summary>
        /// Takes the credits in one conditional UPDATE, so parallel purchases can't spend the same credits twice
        /// (reading, subtracting and saving let N simultaneous requests each buy with one price's worth).
        /// </summary>
        public async Task<bool> Purchase(int credit, int userId)
        {
            if (credit < 0) return false;

            var updated = await _context.Users
                .Where(u => u.Id == userId && u.Credits >= credit)
                .ExecuteUpdateAsync(set => set.SetProperty(u => u.Credits, u => u.Credits - credit));
            if (updated == 0) return false;

            _commandQueueService.QueueCommand(Models.Enums.CommandQueueType.reduce_credits, new Models.CommandTemplate { UserId = userId, Credits = credit });
            return true;
        }

        public async Task<bool> CanPurchase(int credit, int userId)
        {
            var user = await _context.Users.Where(s => s.Id == userId).FirstOrDefaultAsync();
            if (user != null)
            {
                var newCredits = user.Credits - credit;
                if (newCredits >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task LogPurchase(int userId, string kind, string itemName, int credits, int? definitionId = null, int? catalogueItemId = null, string pageFuse = null)
        {
            _context.PurchaseLog.Add(new PurchaseLog
            {
                UserId = userId,
                Kind = kind,
                ItemName = itemName ?? string.Empty,
                Credits = credits,
                DefinitionId = definitionId,
                CatalogueItemId = catalogueItemId,
                PageFuse = pageFuse,
                Source = "web",
                CreatedAt = DateTime.Now,
            });
            await _context.SaveChangesAsync();
        }

        public async Task<Items> PurchaseFurni(int catalogId, int userId)
        {
            var catalogItem = await _context.CatalogueItems.Where(s => s.Id == catalogId).FirstOrDefaultAsync();
            if(catalogItem != null)
            {
                var catalogPage = _context.CataloguePages.Where(s => s.Id == int.Parse(catalogItem.PageId)).FirstOrDefault();
                if(catalogPage != null)
                {
                    if(await Purchase(catalogItem.Price, userId))
                    {
                        var newItem = new Items { DefinitionId = catalogItem.DefinitionId, UserId = userId, CustomData = "" };
                        _context.Items.Add(newItem);
                        await _context.SaveChangesAsync();
                        _commandQueueService.QueueCommand(Models.Enums.CommandQueueType.purchase_furni, new Models.CommandTemplate { UserId = userId, DefinitionId = catalogItem.DefinitionId });
                        var definitionName = await _context.ItemsDefinitions.Where(d => d.Id == catalogItem.DefinitionId).Select(d => d.Name).FirstOrDefaultAsync();
                        await LogPurchase(userId, PurchaseLog.WebFurni, definitionName ?? catalogItem.SaleCode, catalogItem.Price, catalogItem.DefinitionId, catalogItem.Id, catalogPage.Fuse);
                        return newItem;
                    }
                }
            }

            return null;
        }
    }

}
