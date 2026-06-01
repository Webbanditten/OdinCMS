using KeplerCMS.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{

    [Table("cms_menu")]
    public class Menu
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("parent")]
        public int Parent { get; set; }
        [Column("text")]
        public string Text { get; set; }
        [Column("icon")]
        public string Icon { get; set; }
        [Column("href")]
        public string Href { get; set; }
        [Column("order")]
        public int Order { get; set; }
        [Column("state")]
        public string State { get; set; }
        [Column("start_date")]
        public DateTime? StartDate { get; set; }
        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        // True when the menu item is within its (optional) scheduled window right now.
        // An empty bound is open-ended. Bounds are inclusive.
        [NotMapped]
        public bool IsScheduledVisible
        {
            get
            {
                var now = DateTime.Now;
                return (StartDate == null || StartDate <= now)
                    && (EndDate == null || EndDate >= now);
            }
        }
    }
}
