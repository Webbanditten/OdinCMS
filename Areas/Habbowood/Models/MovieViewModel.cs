using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.Habbowood
{
    
    public class MovieViewModel 
    {
        public int Votes { get; set; }
        public Movies Movie { get; set; }
        public List<TopMovie> TopMovies { get; set; }
        public bool CanVote { get; set; }
        public int AvgRating { get; set; }
    }

    public class MovieAndVotes
    {
        public List<MovieVotes> Votes { get; set; }
        public Movies Movie { get; set; }
    }

    public class TopMovie
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
