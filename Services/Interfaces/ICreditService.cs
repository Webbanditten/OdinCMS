using KeplerCMS.Data.Models;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface ICreditService
    {
        Task<bool> RedeemCode(string code, int userId);
        Task<bool> Purchase(int credit, int userId);
        Task<bool> CanPurchase(int credit, int userId);
    }
}
