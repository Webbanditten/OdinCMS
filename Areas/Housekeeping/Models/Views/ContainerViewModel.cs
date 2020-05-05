using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class ContainerAddViewModel
    {
        public string Title { get; set; }
        public string Text { get; set; }
        public int PageId { get; set; }
        public string Type { get; set; }
        public int Column { get; set; }
        public string Theme { get; set; }
        public int Order { get; set; }
    }
    public class ContainerUpdateViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public int PageId { get; set; }
        public string Type { get; set; }
        public int Column { get; set; }
        public string Theme { get; set; }
        public int Order { get; set; }
    }
}
