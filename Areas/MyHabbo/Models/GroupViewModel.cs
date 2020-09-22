using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Areas.MyHabbo.Models
{
    public class GroupViewModel
    {
        public Homes Home { get; set; }
        public GroupMembers GroupMember { get; set; }
    }
}
