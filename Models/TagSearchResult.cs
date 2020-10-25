using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Models
{
    public class TagSearchResult
    {
        public Homes Group {get;set;}
        public Users User { get; set; }
        public List<Tags> Tags { get; set; }
        public string Tag { get; set; }
    }
}
