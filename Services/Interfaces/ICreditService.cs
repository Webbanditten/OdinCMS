using KeplerCMS.Data.Models;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface ICreditService
    {
        Task<bool> RedeemCode(string code, int userId);
        Task<bool> Purchase(int credit, int userId);
        Task<bool> CanPurchase(int credit, int userId);
        Task<Items> PurchaseFurni(int catalogId, int userId);
        /// <summary>Record a website purchase in purchase_log (Odin logs the in-game ones).</summary>
        Task LogPurchase(int userId, string kind, string itemName, int credits, int? definitionId = null, int? catalogueItemId = null, string pageFuse = null);
    }
}
