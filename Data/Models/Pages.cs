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
        public int _DisplayHeader { get; set; }
        [Column("news")]
        public int _News { get; set; }
        [Column("design")]
        public string design { get; set; }

        [NotMapped]
        public bool DisplayHeader
        {
            get { return _DisplayHeader == 1; }
            set { _DisplayHeader = value ? 1 : 0; }
        }

        [NotMapped]
        public bool News
        {
            get { return _News == 1; }
            set { _News = value ? 1 : 0; }
        }
    }
}
