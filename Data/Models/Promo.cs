using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("cms_promo")]
    public class Promo
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("page_id")]
        public int PageId { get; set; }
        [Column("img")]
        public string Img { get; set; }
        [Column("link1_text")]
        public string Link1Text { get; set; }
        [Column("link1_href")]
        public string Link1Href { get; set; }
        [Column("link2_text")]
        public string Link2Text { get; set; }
        [Column("link2_href")]
        public string Link2Href { get; set; }



    }
}
