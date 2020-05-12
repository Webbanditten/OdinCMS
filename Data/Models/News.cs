using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("cms_news")]
    public class News
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("title")]
        public string Title { get; set; }
        [Column("slug")]
        public string Slug { get; set; }
        [Column("teaser")]
        public string Teaser { get; set; }
        [Column("text")]
        public string Text { get; set; }
        [Column("writer")]
        public string Writer { get; set; }
        [Column("publish_date")]
        [Display(Name = "Publishing date")]
        [DataType(DataType.Date)]
        public DateTime PublishDate { get; set; }



    }
}
