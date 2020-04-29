using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IMenuService
    {
        Task<List<Menu>> GetMenu();

    }
}
