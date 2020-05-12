using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface INewsService
    {
        Task<List<News>> GetNews(int from, int amount);
        Task<List<News>> GetAll();
        Task<News> GetBySlug(string slug);
        Task<News> Get(int id);
        Task<News> Add(News model);
        Task<bool> Remove(int id);
        Task<News> Update(News model);

    }
}
