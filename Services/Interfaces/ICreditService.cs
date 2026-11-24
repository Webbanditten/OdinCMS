using KeplerCMS.Data.Models;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface ICreditService
    {
        Task<bool> Purchase(int credit, int userId);
        /// <summary>Gives back credits a purchase took, when the item couldn't be handed over.</summary>
        Task Refund(int credit, int userId);
        Task<bool> CanPurchase(int credit, int userId);
        /// <summary>Record a website purchase in purchase_log (Odin logs the in-game ones).</summary>
        Task LogPurchase(int userId, string kind, string itemName, int credits, int? definitionId = null, int? catalogueItemId = null, string pageFuse = null);
    }
}
