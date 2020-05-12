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

        public async Task<List<Promo>> GetPromos(int pageId)
        {
            return await _context.Promos.Where(s => s.PageId == pageId).OrderBy(s => s.Id).ToListAsync();
        }
    }

}
