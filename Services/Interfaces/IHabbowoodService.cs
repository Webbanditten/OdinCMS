using KeplerCMS.Areas.Habbowood;
using KeplerCMS.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Interfaces
{
    public interface IHabbowoodService
    {
        Task<Movies> GetMovie(int id);
        Task<List<Movies>> GetUsersMovies(int userId);

        Task<Movies> SaveMovie(Movies movie, int? userId);

        Task<bool> CanVote(int movieId, int userId);

        Task<int> GetAvgRating(int movieId);

        Task<bool> Vote(int movieId, int rating, int userId);
        Task<Movies> GetMovieBySession(string key);

        Task<List<TopMovie>> GetTopMovies();
    }
}
