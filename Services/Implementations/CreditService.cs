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

        /// <summary>Gives back credits a purchase took (the item couldn't be handed over); Odin adds them to an online player.</summary>
        public async Task Refund(int credit, int userId)
        {
            if (credit <= 0) return;

            await _context.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(set => set.SetProperty(u => u.Credits, u => u.Credits + credit));
            _commandQueueService.QueueCommand(Models.Enums.CommandQueueType.update_credits, new Models.CommandTemplate { UserId = userId, Credits = credit });
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

    }

}
