using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping.Models.Views
{
    public class MenuAddViewModel
    {
        [Required]
        public string Text { get; set; }
        [Required]
        public string Icon { get; set; }
        [Required]
        public string Href { get; set; }
        [Required]
        public string State { get; set; }
    }
    public class MenuUpdateViewModel
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Icon { get; set; }
        public string Href { get; set; }
        public string State { get; set; }
    }

    public class MenuReArrangeModel
    {
        public int Id { get; set; }
        public List<MenuReArrangeModel> Children { get; set; }
    }
}
