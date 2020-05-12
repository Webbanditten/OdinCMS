using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IPromoService
    {
        Task<List<Promo>> GetPromos(int pageId);
    }
}
