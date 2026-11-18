using Isopoh.Cryptography.Argon2;
using KeplerCMS.Areas.Habbowood;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class HabbowoodService : IHabbowoodService
    {
        private readonly DataContext _context;

        public HabbowoodService(DataContext context)
        {
            _context = context;
        }
        public async Task<bool> CanVote(int movieId, int userId)
        {
            var currentVote = await _context.MovieVotes.Where(s => s.MovieId == movieId && s.UserId == userId).FirstOrDefaultAsync();
            if(currentVote != null) return false;

            return true;
        }

        public async Task<int> GetAvgRating(int movieId)
        {
            var votes = await _context.MovieVotes.Where(s => s.MovieId == movieId).ToListAsync();
            return Convert.ToInt32((votes.Count() > 0) ? Math.Floor(votes.Average(s => s.Rating)) : 0);
        }

        public async Task<Movies> GetMovie(int id)
        {
            return await _context.Movies.Where(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Movies> GetMovieBySession(string key)
        {
            return await _context.Movies.Where(s => s.SessionId == key).FirstOrDefaultAsync();
        }

        public async Task<List<TopMovie>> GetTopMovies()
        {
            var avg = await _context.MovieVotes
                  .GroupBy(g => g.MovieId, c => c.Rating)
                  .Select(g => new
                  {
                      MovieId = g.Key,
                      Average = g.Average()
                  }).Take(10).ToListAsync();

            List<TopMovie> movies = new List<TopMovie>();
            foreach(var a in avg)
            {
                var movie = await GetMovie(a.MovieId);
                movies.Add( new TopMovie { Name = movie.Name, Id = movie.Id });
            }

           return movies;
        }

        public async Task<List<HabbowoodListMovie>> GetMovieList(string kind, int take)
        {
            var movies = await _context.Movies.AsNoTracking().Where(m => m.IPublished == 1)
                .Select(m => new { m.Id, m.Name, m.UserId }).ToListAsync();
            var ids = movies.Select(m => m.Id).ToList();
            var votes = await _context.MovieVotes.AsNoTracking().Where(v => ids.Contains(v.MovieId))
                .GroupBy(v => v.MovieId).Select(g => new { MovieId = g.Key, Average = g.Average(v => (double)v.Rating), Count = g.Count() }).ToListAsync();
            var userIds = movies.Select(m => m.UserId).Distinct().ToList();
            var names = await _context.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username);

            var list = movies.Select(m =>
            {
                var v = votes.FirstOrDefault(x => x.MovieId == m.Id);
                return new HabbowoodListMovie(m.Id, string.IsNullOrWhiteSpace(m.Name) ? "#" + m.Id : m.Name,
                    names.GetValueOrDefault(m.UserId), v?.Average ?? 0, v?.Count ?? 0);
            });
            list = kind == "top"
                ? list.OrderByDescending(m => m.Votes > 0).ThenByDescending(m => m.Rating).ThenByDescending(m => m.Votes).ThenByDescending(m => m.Id)
                : list.OrderByDescending(m => m.Id);
            return list.Take(take).ToList();
        }

        public async Task<List<HabbowoodUserMovie>> GetUserMovieList(int userId)
        {
            var movies = await _context.Movies.AsNoTracking().Where(m => m.UserId == userId).OrderByDescending(m => m.Id).ToListAsync();
            var ids = movies.Select(m => m.Id).ToList();
            var votes = await _context.MovieVotes.AsNoTracking().Where(v => ids.Contains(v.MovieId))
                .GroupBy(v => v.MovieId).Select(g => new { MovieId = g.Key, Average = g.Average(v => (double)v.Rating), Count = g.Count() }).ToListAsync();
            return movies.Select(m =>
            {
                var v = votes.FirstOrDefault(x => x.MovieId == m.Id);
                return new HabbowoodUserMovie(m.Id, string.IsNullOrWhiteSpace(m.Name) ? "#" + m.Id : m.Name, MovieAttribute(m.Data, "subtitle"),
                    m.Published, !string.IsNullOrEmpty(m.Data), v?.Average ?? 0, v?.Count ?? 0);
            }).ToList();
        }

        /// <summary>An attribute of the movie element in the editor's XML (name, subtitle, director, ...), or null.</summary>
        public static string MovieAttribute(string data, string attribute)
        {
            if (string.IsNullOrEmpty(data)) return null;
            try
            {
                using var reader = System.Xml.XmlReader.Create(new System.IO.StringReader(data), new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit });
                while (reader.Read())
                {
                    if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "movie") return reader.GetAttribute(attribute);
                }
            }
            catch (System.Xml.XmlException)
            {
            }
            return null;
        }

        public async Task<Movies> GetDraft(int userId, int? movieId)
        {
            if (movieId != null)
            {
                var own = await _context.Movies.FirstOrDefaultAsync(m => m.Id == movieId && m.UserId == userId && m.IPublished == 0);
                if (own != null) return own;
            }
            var blank = await _context.Movies.Where(m => m.UserId == userId && m.IPublished == 0 && (m.Data == null || m.Data == ""))
                .OrderByDescending(m => m.Id).FirstOrDefaultAsync();
            return blank ?? await SaveMovie(new Movies(), userId);
        }

        public async Task<bool> DeleteMovie(int movieId, int userId)
        {
            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == movieId && m.UserId == userId);
            if (movie == null) return false;
            _context.MovieVotes.RemoveRange(await _context.MovieVotes.Where(v => v.MovieId == movieId).ToListAsync());
            _context.Movies.Remove(movie);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Movies>> GetUsersMovies(int userId)
        {
            return await _context.Movies.Where(s => s.UserId == userId).ToListAsync();
        }

        public async Task<Movies> SaveMovie(Movies movie, int? userId)
        {
            var dbMovie = await GetMovie(movie.Id);
            if(dbMovie != null)
            {
                dbMovie.Data = (movie.Data != null) ? movie.Data : dbMovie.Data;
                dbMovie.Name = (movie.Name != null) ? movie.Name : dbMovie.Name;
                dbMovie.Published = (movie.Published) ? movie.Published : false;

                _context.Update(dbMovie);
                await _context.SaveChangesAsync();
                return dbMovie;
            } else
            {
                var newMovie = new Movies { Name = movie.Name, Data = movie.Data, Published = false, UserId = userId ?? default, SessionId = Guid.NewGuid().ToString() };
                _context.Add(newMovie);
                await _context.SaveChangesAsync();
                return newMovie;
            }

        }

        public async Task<bool> Vote(int movieId, int rating,  int userId)
        {
            var canVote = await CanVote(movieId, userId);
            if(canVote)
            {
                _context.Add(new MovieVotes { MovieId = movieId, Rating = rating, UserId = userId });
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }

}
