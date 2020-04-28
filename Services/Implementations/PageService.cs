using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System;
using System.Linq;

namespace KeplerCMS.Services.Implementations
{
    public class PageService : IPageService
    {
        private DataContext _context;

        public PageService(DataContext context)
        {
            _context = context;
        }

        public Page GetPageBySlug(string slug)
        {
            var pageDetails = _context.Pages.Where(p => p.Slug == slug).FirstOrDefault();
            if(pageDetails == null)
            {
                return null;
            }
            var containers = _context.Containers.Where(c => c.PageId == pageDetails.Id).ToList();
            return new Page { Details = pageDetails, Containers = containers };
        }
    }

}
