using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class PageAddViewModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Slug { get; set; }

        public string NewsHeader { get; set; }
        public bool DisplayHeader { get; set; }
        public bool News { get; set; }
        [Required]
        public string Design { get; set; }
        public bool Hidden { get; set; }
    }
    public class PageUpdateViewModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Slug { get; set; }
        public string NewsHeader { get; set; }
        public bool DisplayHeader { get; set; }
        public bool News { get; set; }
        [Required]
        public string Design { get; set; }

        public bool Hidden { get; set; }
    }
    public class PageGrid
    {
        public int PageId { get; set; }
        public List<PageColumn> Columns { get; set; }
    }

    public class PageColumn
    {
        public int Column { get;set; }
        public List<PageGridContainer> Containers { get; set; }
    }

    public class PageGridContainer
    {
        public int Id { get; set; }
    }
    
    public class PromoReArrange
    {
        public int Id { get; set; }
    }
    
    public class PromoReArrangePostModel
    {
        public int PageId { get; set; }
        public List<PromoReArrange> Promos { get; set; }
    }


}
