using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface ICatalogueService
    {
        Task<List<CataloguePages>> GetCataloguePages();
        Task<CataloguePages> Get(int id);
        Task<bool> Remove(int id);
        Task<bool> ReArrange(List<CatalogueReArrangeModel> model);

        Task<List<CatalogueItems>> GetCataloguePageItems(string pageId);
    }
}
