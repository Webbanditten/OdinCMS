using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class MenuService : IMenuService
    {
        private readonly DataContext _context;

        public MenuService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<Menu>> GetMenu()
        {
            return await _context.Menu.ToListAsync();
        }
    }

}
