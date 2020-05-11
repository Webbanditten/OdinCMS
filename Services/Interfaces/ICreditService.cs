using KeplerCMS.Data.Models;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface ICreditService
    {
        Task<bool> RedeemCode(string code, int userId);
    }
}
