using KeplerCMS.Areas.MyHabbo.Models;
using KeplerCMS.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Models
{
    public class GroupDirectory
    {
        public List<Homes> Recent { get; set; }
        public List<Homes> Hotel { get; set; }
        public List<Homes> Active { get; set; }
        public List<GroupViewModel> MyGroups { get; set; }

    }
}
