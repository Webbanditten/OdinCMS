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
    public class NewsService : INewsService
    {
        private readonly DataContext _context;

        public NewsService(DataContext context)
        {
            _context = context;
        }

        public Task<News> Add(News model)
        {
            throw new System.NotImplementedException();
        }

        public Task<News> Get(int id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<List<News>> GetAll()
        {
            return await _context.News.OrderByDescending(s => s.PublishDate).ToListAsync();
        }

        public async Task<News> GetBySlug(string slug)
        {
            return await _context.News.Where(s => s.Slug.ToLower() == slug.ToLower()).FirstOrDefaultAsync();
        }

        public async Task<List<News>> GetNews(int from, int amount)
        {
            return await _context.News.OrderByDescending(s => s.PublishDate).Skip(from).Take(amount).ToListAsync();
        }

        public Task<bool> Remove(int id)
        {
            throw new System.NotImplementedException();
        }

        public Task<News> Update(News model)
        {
            throw new System.NotImplementedException();
        }
    }

}
