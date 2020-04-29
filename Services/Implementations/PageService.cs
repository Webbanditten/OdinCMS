using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class PageService : IPageService
    {
        private DataContext _context;

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
    }

}
