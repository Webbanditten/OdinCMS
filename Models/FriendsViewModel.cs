using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Models
{
    public class FriendsViewModel
    {
        public Users UserDetails { get; set; }
        public Friends FriendDetails { get; set; }
    }
}
