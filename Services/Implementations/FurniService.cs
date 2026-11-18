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
            return await WithImageSprites(await _context.ItemsDefinitions.Where(x => x.Sprite.Contains(query) || x.Name.Contains(query)).ToListAsync());
        }

        public async Task<ItemsDefinitions> Get(int id)
        {
            var furni = await _context.ItemsDefinitions.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (furni != null) await WithImageSprites(new List<ItemsDefinitions> { furni });
            return furni;
        }

        public async Task<List<ItemsDefinitions>> Get(int[] ids)
        {
            return await WithImageSprites(await _context.ItemsDefinitions.Where(x => ids.Contains(x.Id)).ToListAsync());
        }

        private async Task<List<ItemsDefinitions>> WithImageSprites(List<ItemsDefinitions> furni)
        {
            var posters = await PosterSprites(_context, furni.Where(f => f.Sprite == "poster").Select(f => f.Id).ToList());
            foreach (var f in furni) f.ImageSprite = posters.GetValueOrDefault(f.Id);
            return furni;
        }

        /// <summary>
        /// What the furni imager needs for poster definitions: all posters have the sprite "poster" and are told apart by
        /// their catalogue item's special sprite id (Odin sends the class "poster 27"). A definition handed out without a
        /// catalogue item (rewards, vouchers) becomes the poster of its first catalogue row, as Odin picks it
        /// (CatalogueDao.getCatalogueItemByItemDefinition). Definitions without a numbered row aren't in the result.
        /// </summary>
        public static async Task<Dictionary<int, string>> PosterSprites(DataContext context, ICollection<int> definitionIds)
        {
            if (definitionIds.Count == 0) return new Dictionary<int, string>();
            var posters = await context.ItemsDefinitions.Where(d => definitionIds.Contains(d.Id) && d.Sprite == "poster").Select(d => d.Id).ToListAsync();
            if (posters.Count == 0) return new Dictionary<int, string>();
            var rows = await context.CatalogueItems.Where(c => posters.Contains(c.DefinitionId))
                .Select(c => new { c.Id, c.DefinitionId, c.ItemSpecialSpriteId }).ToListAsync();
            return rows.GroupBy(r => r.DefinitionId).Select(g => g.OrderBy(r => r.Id).First())
                .Where(r => r.ItemSpecialSpriteId > 0)
                .ToDictionary(r => r.DefinitionId, r => "poster" + r.ItemSpecialSpriteId);
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
