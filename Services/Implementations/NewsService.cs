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

        public async Task<News> Add(News model)
        {
            await _context.News.AddAsync(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<News> Get(int id)
        {
            return await _context.News.Where(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> Remove(int id)
        {
            _context.News.Remove(await Get(id));
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<News> Update(News model)
        {
            _context.News.Update(model);
            await _context.SaveChangesAsync();
            return model;
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

    }

}
