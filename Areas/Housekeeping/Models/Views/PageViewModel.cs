using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class PageAddViewModel
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public bool DisplayHeader { get; set; }
        public bool News { get; set; }
        public string Design { get; set; }
    }
    public class PageUpdateViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public bool DisplayHeader { get; set; }
        public bool News { get; set; }
        public string Design { get; set; }
    }
}
