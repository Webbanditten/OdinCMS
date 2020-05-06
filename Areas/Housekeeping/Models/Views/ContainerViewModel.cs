using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class ContainerAddViewModel
    {
        [Required]
        public string Title { get; set; }
        public string Text { get; set; }
        [Required]
        public int PageId { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]
        public int Column { get; set; }
        [Required]
        public string Theme { get; set; }
    }
    public class ContainerUpdateViewModel
    {
        [Required]
        public int PageId { get; set; }
        [Required]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string Text { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]
        public string Theme { get; set; }
    }
}
