using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface ICatalogueService
    {
        Task<List<CataloguePages>> GetCataloguePages();

        Task<List<CataloguePageEdit>> GetPages();
        Task<CataloguePageEdit> GetPage(int id);
        Task<int> SavePage(CataloguePageEdit edit);
        Task DeletePage(int id);
        Task ReorderPages(List<int> ids);
        Task<int> SetLibraryImages(IEnumerable<string> names);
        Task SaveTree(List<CatalogueTreeNode> nodes);

        Task<List<CatalogueProductEdit>> GetProducts(int pageId);
        Task<List<string>> DuplicateSaleCodes(IEnumerable<string> saleCodes);
        Task<int> SaveProduct(CatalogueProductEdit edit);
        Task<List<int>> AddProducts(int pageId, List<int> definitionIds);
        Task RemoveProduct(int id, int pageId);
        Task ReorderProducts(int pageId, List<int> ids);

        Task<List<CatalogueFurni>> SearchFurni(string query, int limit = 80);
        Task<CatalogueFurni> GetFurni(int id);
        Task<bool> SpriteExists(string sprite, int exceptId);
        Task<int> SaveFurni(CatalogueFurni edit);
        Task<int> MaxSpriteId();
    }
}
