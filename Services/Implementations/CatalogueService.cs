using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class CatalogueService : ICatalogueService
    {
        private readonly DataContext _context;

        public CatalogueService(DataContext context)
        {
            _context = context;
        }

        public Task<CataloguePages> Get(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<List<CataloguePages>> GetCataloguePages()
        {
            return await _context.CataloguePages.ToListAsync();
        }

        public Task<bool> Remove(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<bool> ReArrange(List<CatalogueReArrangeModel> model)
        {

            for (int i = 0; i < model.Count; i++)
            {
                var item = model[i];
                var dbItem = await _context.CataloguePages.Where(m => m.Id == item.Id).FirstOrDefaultAsync();
                dbItem.OrderId = i;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
