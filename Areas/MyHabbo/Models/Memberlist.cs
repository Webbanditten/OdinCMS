using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.MyHabbo.Models
{
    public class Memberlist
    {
        public string Search { get; set; }
        public int PageNumber { get; set; }
        public List<Member> Members { get; set; }
    }

    public class Member {
        public GroupMembers Membership { get; set; }
        public Users UserDetails { get; set; }
    }
}
