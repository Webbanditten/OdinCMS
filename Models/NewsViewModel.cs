using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Models
{
    public class NewsViewModel
    {
        public List<News> LatestNews { get; set; }
        public News News { get; set; }
    }
}
