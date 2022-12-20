using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class UsersViewModel
    {
        public IEnumerable<KeplerCMS.Data.Models.Users> Users { get; set; }
        public int CurrentPage { get; set; }
        public string Filter { get; set; }
        public int TotalPages { get; set; }
        public string Search { get; set; }
        public string Letter { get; set; }

    }
    
    public class UsersSearchModel
    {
        public IEnumerable<Users> Users { get; set; }
        public int TotalResults { get; set; }
    }
}
