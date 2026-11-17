using System.Threading.Tasks;
using KeplerCMS.Models;

namespace KeplerCMS.Services.Interfaces
{
    /// <summary>The catalogue as the website hands out or sells furni (vouchers, the {buy_furni_ID} widget).</summary>
    public interface ICatalogueFurniService
    {
        /// <summary>Every catalogue item with what it gives and whether players can buy it. Cached for the request.</summary>
        Task<CatalogueFurniCatalogue> Load();

        /// <summary>
        /// Puts one furni in the user's hand the way Odin's CatalogueManager.purchase does without extra data, inside the
        /// DataContext's current transaction. Returns film to add to the user (camera 2, a film item 5 and no furni).
        /// </summary>
        Task<int> Give(int userId, string username, CatalogueFurniPart part);
    }
}
