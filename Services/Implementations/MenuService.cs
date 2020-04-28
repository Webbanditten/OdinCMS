using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace KeplerCMS.Services.Implementations
{
    public class MenuService : IMenuService
    {
        private readonly DataContext _context;

        public MenuService(DataContext context)
        {
            _context = context;
        }

        public List<Menu> GetMenu()
        {
            return _context.Menu.ToList();
        }
    }

}
