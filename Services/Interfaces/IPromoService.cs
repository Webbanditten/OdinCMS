using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IPromoService
    {
        Task<List<Promo>> GetPromos(int pageId);
        Task<List<Promo>> GetAll();
        Task<Promo> Get(int id);
        Task<Promo> Add(Promo model);
        Task<bool> Remove(int id);
        Task<Promo> Update(Promo model);
    }
}
