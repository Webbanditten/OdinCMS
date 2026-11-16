using Isopoh.Cryptography.Argon2;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class FurniService : IFurniService
    {
        private readonly DataContext _context;
        public FurniService(DataContext context, IUserService userService)
        {
            _context = context;
        }

        public async Task<List<ItemsDefinitions>> Search(string query)
        {
            return await _context.ItemsDefinitions.Where(x => x.Sprite.Contains(query) || x.Name.Contains(query)).ToListAsync();
        }

        public async Task<ItemsDefinitions> Get(int id)
        {
            return await _context.ItemsDefinitions.Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<ItemsDefinitions>> Get(int[] ids)
        {
            return await _context.ItemsDefinitions.Where(x => ids.Contains(x.Id)).ToListAsync();
        }

        /// <summary>
        /// The furni bought most in the last 30 days, from purchase_log: paid catalogue and website purchases
        /// for yourself. Gifts, purchases from staff pages and purchases by staff accounts (rank above 0) don't
        /// count, nor do items handed out by housekeeping, rewards or vouchers (they never enter the log).
        /// </summary>
        public async Task<PopularFurniContainer> GetPopularFurni()
        {
            var since = DateTime.Now.AddDays(-30);

            var top = await (from purchase in _context.PurchaseLog
                             join buyer in _context.Users on purchase.UserId equals buyer.Id
                             where purchase.CreatedAt >= since
                                   && (purchase.Kind == PurchaseLog.Catalogue || purchase.Kind == PurchaseLog.WebFurni)
                                   && purchase.Credits > 0
                                   && purchase.DefinitionId != null
                                   && (purchase.PageFuse == null || PurchaseLog.PlayerPageFuses.Contains(purchase.PageFuse.ToLower()))
                                   && buyer.Rank == 0
                             group purchase by purchase.DefinitionId into grouped
                             orderby grouped.Count() descending
                             select new { DefinitionId = grouped.Key.Value, Purchases = grouped.Count() })
                            .FirstOrDefaultAsync();

            if (top == null)
            {
                return null;
            }

            return await _context.ItemsDefinitions
                .Where(d => d.Id == top.DefinitionId)
                .Select(d => new PopularFurniContainer
                {
                    DefinitionId = d.Id,
                    Name = d.Name,
                    Sprite = d.Sprite,
                    SpriteId = d.SpriteId,
                    Amount = top.Purchases,
                })
                .FirstOrDefaultAsync();
        }
    }
}
