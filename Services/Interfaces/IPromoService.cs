using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;

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
        Task<bool> ReArrange(PromoReArrangePostModel model);
    }
}
