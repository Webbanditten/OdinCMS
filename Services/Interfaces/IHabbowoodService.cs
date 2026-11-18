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

        /// <summary>A player's own movies for Mine film, newest first.</summary>
        Task<List<HabbowoodUserMovie>> GetUserMovieList(int userId);

        /// <summary>
        /// The movie the studio opens: the player's own unpublished movie movieId, else their newest unpublished one that
        /// has nothing in it yet, else a new one.
        /// </summary>
        Task<Movies> GetDraft(int userId, int? movieId);

        /// <summary>Deletes the player's own movie and its votes.</summary>
        Task<bool> DeleteMovie(int movieId, int userId);
    }

    public record HabbowoodUserMovie(int Id, string Name, string Subtitle, bool Published, bool HasData, double Rating, int Votes);

    public record HabbowoodListMovie(int Id, string Name, string Director, double Rating, int Votes);
}
