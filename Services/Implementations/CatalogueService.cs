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
            var cataloguePages = await _context.CataloguePages.ToListAsync();
            for (int i = 0; i < model.Count; i++)
            {
                var item = model[i];
                var dbItem = cataloguePages.Where(m => m.Id == item.Id).FirstOrDefault();
                dbItem.OrderId = i;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CatalogueItems>> GetCataloguePageItems(string pageId)
        {
            var productList = await (from products in _context.CatalogueItems
                                  join itemdef in _context.ItemsDefinitions
                                  on products.DefinitionId equals itemdef.Id
                                  where products.PageId == pageId
                                  select new CatalogueItems
                                  {
                                      Id = products.Id,
                                        DefinitionId = products.DefinitionId,
                                        PageId = products.PageId,
                                        SaleCode = products.SaleCode,
                                        Amount = products.Amount,
                                        IIsHidden = products.IIsHidden,
                                        IIsPackage = products.IIsPackage,
                                        Price = products.Price,
                                        ItemSpecialSpriteId = products.ItemSpecialSpriteId,
                                        Order = products.Order,
                                        PackageDescription = products.PackageDescription,
                                        PackageName = products.PackageName,
                                        ItemDefinition = itemdef

                                  }).ToListAsync();
            return productList;
        }
    }
}
