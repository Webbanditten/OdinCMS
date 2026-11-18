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

        /// <summary>
        /// Published movies for the site's Habbowood boxes ({habbowood_top}, {habbowood_new}): "top" = best average rating
        /// first (more votes first between equals; unrated last), "new" = newest first.
        /// </summary>
        Task<List<HabbowoodListMovie>> GetMovieList(string kind, int take);
    }

    public record HabbowoodListMovie(int Id, string Name, string Director, double Rating, int Votes);
}
