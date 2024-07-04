using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class PromoService : IPromoService
    {
        private readonly DataContext _context;

        public PromoService(DataContext context)
        {
            _context = context;
        }

        public async Task<Promo> Add(Promo model)
        {
            await _context.Promos.AddAsync(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<Promo> Get(int id)
        {
            return await _context.Promos.Where(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<Promo>> GetAll()
        {
            return await _context.Promos.ToListAsync();
        }



        public async Task<List<Promo>> GetPromos(int pageId)
        {
            return await _context.Promos.Where(s => s.PageId == pageId).OrderBy(s => s.Id).ToListAsync();
        }

        public async Task<bool> Remove(int id)
        {
            _context.Promos.Remove(await Get(id));
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Promo> Update(Promo model)
        {
            _context.Promos.Update(model);
            await _context.SaveChangesAsync();
            return model;
        }
        
        public async Task<bool> ReArrange(PromoReArrangePostModel model)
        {

            for (var i = 0; i < model.Promos.Count; i++)
            {
                var item = model.Promos[i];
                var dbItem = await _context.Promos.Where(m => m.Id == item.Id).FirstOrDefaultAsync();
                dbItem.Order = i;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }

}
