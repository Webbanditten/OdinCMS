using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("cms_pages")]
    public class Pages
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("slug")]
        public string Slug { get; set; }
        [Column("display_header")]
        public int IDisplayHeader { get; set; }
        [Column("news")]
        public int INews { get; set; }
        [Column("design")]
        public string Design { get; set; }
        [Column("news_header")]
        public string NewsHeader { get; set; }
        [NotMapped]
        public bool DisplayHeader
        {
            get { return IDisplayHeader == 1; }
            set { IDisplayHeader = value ? 1 : 0; }
        }

        [NotMapped]
        public bool News
        {
            get { return INews == 1; }
            set { INews = value ? 1 : 0; }
        }
        
        [Column("hidden")]
        public int IHidden { get; set; }
        
        [NotMapped]
        public bool Hidden
        {
            get => IHidden == 1;
            set => IHidden = value ? 1 : 0;
        }
    }
}
