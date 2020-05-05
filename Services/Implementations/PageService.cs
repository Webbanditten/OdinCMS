using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        public async Task<Pages> GetPageById(int id)
        {
            return await _context.Pages.Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Containers> GetContainerById(int id)
        {
            return await _context.Containers.Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async void AddDetails(PageAddViewModel model)
        {
          
            await _context.Pages.AddAsync(new Pages
            {
                Name = model.Name,
                Slug = model.Slug,
                DisplayHeader = model.DisplayHeader,
                News = model.News,
                Design = model.Design
            });
            await _context.SaveChangesAsync();

        }
        public async void AddContainer(ContainerAddViewModel model)
        {
            await _context.Containers.AddAsync(new Containers
            {
                Title = model.Title,
                Text = model.Text,
                PageId = model.PageId,
                Type = model.Type,
                Column = model.Column,
                Theme = model.Theme,
                Order = model.Order
            });
            await _context.SaveChangesAsync();
        }


        public async void Remove(int id)
        {
            _context.Containers.RemoveRange(await _context.Containers.Where(s => s.PageId == id).ToListAsync());
            _context.Pages.Remove(await _context.Pages.Where(m => m.Id == id).FirstOrDefaultAsync());
            await _context.SaveChangesAsync();

        }

        public async void RemoveContainer(int id)
        {
            _context.Containers.Remove(await _context.Containers.Where(m => m.Id == id).FirstOrDefaultAsync());
            await _context.SaveChangesAsync();

        }

        public async void UpdateDetails(PageUpdateViewModel model)
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
        }

        public async void UpdateContainer(ContainerUpdateViewModel model)
        {
            var item = await _context.Containers.Where(m => m.Id == model.Id).FirstOrDefaultAsync();
            if (item != null)
            {
                item.Title = model.Title;
                item.Text = model.Text;
                item.Type = model.Type;
                item.Column = model.Column;
                item.Theme = model.Theme;
                item.Order = model.Order;

                _context.Containers.Update(item);
                await _context.SaveChangesAsync();
            }
        }
    }

}
