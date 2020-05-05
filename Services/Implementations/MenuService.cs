using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public async Task<Menu> Add(MenuAddViewModel model)
        {
            var menu = new Menu
            {
                Parent = 0,
                Text = model.Text,
                Icon = model.Icon,
                Href = model.Href,
                Order = 0,
                State = model.State
            };
            await _context.Menu.AddAsync(menu);
            await _context.SaveChangesAsync();
            return menu;

        }

        public async Task<bool> Remove(int id)
        {
            _context.Menu.Remove(await _context.Menu.Where(m => m.Id == id).FirstOrDefaultAsync());
            await _context.SaveChangesAsync();
            return true;

        }

        public async Task<Menu> Update(MenuUpdateViewModel model)
        {
            var item = await _context.Menu.Where(m => m.Id == model.Id).FirstOrDefaultAsync();
            if(item != null)
            {
                item.Text = model.Text;
                item.Icon = model.Icon;
                item.Href = model.Href;
                item.State = model.State;
                _context.Menu.Update(item);
                await _context.SaveChangesAsync();
            }
            return item;
        }

        public async Task<bool> ReArrange(List<MenuReArrangeModel> model)
        {

            for (int i = 0; i < model.Count; i++)
            {
                var item = model[i];
                var hasChildren = item.Children != null;
                var dbItem = await _context.Menu.Where(m => m.Id == item.Id).FirstOrDefaultAsync();
                dbItem.Parent = 0;
                dbItem.Order = i;

                if(hasChildren)
                {
                    for (int y = 0; y < item.Children.Count; y++)
                    {
                        var subItem = item.Children[y];
                        var dbSubItem = await _context.Menu.Where(m => m.Id == subItem.Id).FirstOrDefaultAsync();
                        dbSubItem.Order = y;
                        dbSubItem.Parent = item.Id;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Menu> Get(int id)
        {
            return await _context.Menu.Where(s => s.Id == id).FirstOrDefaultAsync();
        }
    }

}
