using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Models;

namespace KeplerCMS.Services.Interfaces
{
    public enum FurniPurchaseOutcome
    {
        Bought,
        NotEnoughCredits,
        /// <summary>Not publicly for sale (hidden, or only on staff/club pages), or the website can't hand it out.</summary>
        NotForSale,
        NotFound,
    }

    /// <summary>
    /// Furni bought on the website: the {buy_furni_ID} widget in news and page boxes (and older hand-written buttons with
    /// class "buy-btn"). Only what any player can buy in the game's catalogue is for sale.
    /// </summary>
    public interface IFurniShopService
    {
        Task<CatalogueFurniEntry> Get(int catalogueItemId);

        /// <summary>Catalogue items for the editor's picker by name or sale code, publicly buyable ones first.</summary>
        Task<List<CatalogueFurniEntry>> Search(string query);

        /// <summary>Why <paramref name="entry"/> can't be bought on the website, or null if it can.</summary>
        string WhyNotForSale(CatalogueFurniEntry entry);

        Task<FurniPurchaseOutcome> Purchase(int catalogueItemId, int userId);
    }
}
