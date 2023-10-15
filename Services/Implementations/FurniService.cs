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

        public async Task<PopularFurniContainer> GetPopularFurni()
        {
            var lastMonth = DateTime.Now.AddMonths(-12);

            var query = from item in _context.Items
                join definition in _context.ItemsDefinitions
                    on item.DefinitionId equals definition.Id into joined
                from d in joined.DefaultIfEmpty()
                where item.CreatedAt >= lastMonth && d != null && !d.Behavior.Contains("present")
                group item by item.DefinitionId into grouped
                orderby grouped.Count() descending
                select new PopularFurniContainer
                {
                    Name = grouped.FirstOrDefault().Definition.Name, // Assuming you have a navigation property for Name in the Definition entity
                    Sprite = grouped.FirstOrDefault().Definition.Sprite, // Assuming you have a navigation property for Sprite in the Definition entity
                    SpriteId = grouped.FirstOrDefault().Definition.SpriteId, // Assuming you have a navigation property for SpriteId in the Definition entity
                    DefinitionId = grouped.Key,
                };

            var mostBoughtProduct = query.FirstOrDefault();

            if (mostBoughtProduct != null)
            {
                // Calculate purchase count separately using a subquery
                var purchaseCount = (from item in _context.Items
                    where item.DefinitionId == mostBoughtProduct.DefinitionId && item.CreatedAt >= lastMonth
                    select item).Count();
    
                mostBoughtProduct.Amount = purchaseCount;
            }

            return mostBoughtProduct;
        }
    }
}
