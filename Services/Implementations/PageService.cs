using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class PageService : IPageService
    {
        private readonly DataContext _context;

        public PageService(DataContext context)
        {
            _context = context;
        }

        public async Task<Page> GetPageBySlug(string slug)
        {
            var pageDetails = await _context.Pages.Where(p => p.Slug == slug).FirstOrDefaultAsync();
            if(pageDetails == null)
            {
                return null;
            }
            var containers = _context.Containers.Where(c => c.PageId == pageDetails.Id).ToList();
            return new Page { Details = pageDetails, Containers = containers };
        }
        public async Task<Page> GetPageObjById(int id)
        {
            var pageDetails = await _context.Pages.Where(p => p.Id == id).FirstOrDefaultAsync();
            if (pageDetails == null)
            {
                return null;
            }
            var containers = _context.Containers.Where(c => c.PageId == pageDetails.Id).ToList();
            return new Page { Details = pageDetails, Containers = containers };
        }

        public async Task<IEnumerable<Pages>> GetAll()
        {
            return await _context.Pages.ToListAsync();
        }
        public async Task<Pages> GetPageById(int id)
        {
            return await _context.Pages.Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Containers> GetContainerById(int id)
        {
            return await _context.Containers.Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Pages> AddDetails(PageAddViewModel model)
        {
            var page = new Pages
            {
                Name = model.Name,
                Slug = model.Slug,
                DisplayHeader = model.DisplayHeader,
                News = model.News,
                Design = model.Design
            };
            await _context.Pages.AddAsync(page);
            await _context.SaveChangesAsync();
            return page;

        }
        public async Task<Containers> AddContainer(ContainerAddViewModel model)
        {
            var container = new Containers
            {
                Title = model.Title,
                Text = model.Text,
                PageId = model.PageId,
                Type = model.Type,
                Column = model.Column,
                Theme = model.Theme,
                Order = 0
            };
            await _context.Containers.AddAsync(container);
            await _context.SaveChangesAsync();
            return container;
        }


        public async Task<bool> Remove(int id)
        {
            _context.Containers.RemoveRange(await _context.Containers.Where(s => s.PageId == id).ToListAsync());
            _context.Pages.Remove(await _context.Pages.Where(m => m.Id == id).FirstOrDefaultAsync());
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveContainer(int id)
        {
            _context.Containers.Remove(await _context.Containers.Where(m => m.Id == id).FirstOrDefaultAsync());
            await _context.SaveChangesAsync();
            return true;

        }

        public async Task<Pages> UpdateDetails(PageUpdateViewModel model)
        {
            var item = await _context.Pages.Where(m => m.Id == model.Id).FirstOrDefaultAsync();
            if (item != null)
            {
                item.Name = model.Name;
                item.Slug = model.Slug;
                item.DisplayHeader = model.DisplayHeader;
                item.News = model.News;
                item.Design = model.Design;
                _context.Pages.Update(item);
                await _context.SaveChangesAsync();
            }
            return item;
        }

        public async Task<Containers> UpdateContainer(ContainerUpdateViewModel model)
        {
            var item = await _context.Containers.Where(m => m.Id == model.Id).FirstOrDefaultAsync();
            if (item != null)
            {
                item.Title = model.Title;
                item.Text = model.Text;
                item.Type = model.Type;
                item.Theme = model.Theme;

                _context.Containers.Update(item);
                await _context.SaveChangesAsync();
            }
            return item;
        }

        public async Task<bool> ArrangeContainers(PageGrid grid)
        {

            foreach(var column in grid.Columns)
            {
                for (int i = 0; i < column.Containers.Count; i++)
                {
                    var container = column.Containers[i];
                    var item = await _context.Containers.Where(s => s.Id == container.Id).FirstOrDefaultAsync();
                    if(item != null)
                    {
                        item.Column = column.Column;
                        item.Order = i;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }

}
