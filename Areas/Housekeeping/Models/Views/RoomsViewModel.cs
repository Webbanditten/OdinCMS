using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class SearchRoomsViewModel
    {
        public IEnumerable<Rooms> Rooms { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string Search { get; set; }

    }
    
        public class SearchRoomsModel
        {
            public IEnumerable<Rooms> Rooms { get; set; }
            public int TotalResults { get; set; }
        }
}
