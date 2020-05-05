using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IMenuService
    {
        Task<List<Menu>> GetMenu();
        Task<Menu> Get(int id);
        Task<Menu> Add(MenuAddViewModel model);
        Task<bool> Remove(int id);
        Task<Menu> Update(MenuUpdateViewModel model);
        Task<bool> ReArrange(List<MenuReArrangeModel> model);
    }
}
