using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;

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
    
    
    public class ManageUserViewModel
    {
        public Users User { get; set; }
        public List<Users> Friends { get; set; }
        public Rank Rank { get; set; }
        public IEnumerable<Rank> Ranks { get; set; }
        public string Message { get; set; }
    }
}
