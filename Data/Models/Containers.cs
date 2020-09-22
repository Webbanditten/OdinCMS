using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("cms_containers")]
    public class Containers
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("title")]
        public string Title { get; set; }
        [Column("text")]
        public string Text { get; set; }
        [Column("page_id")]
        public int PageId { get; set; }
        [Column("type")]
        public string Type { get; set; }
        [Column("column")]
        public int Column { get; set; }
        [Column("theme")]
        public string Theme { get; set; }
        [Column("order")]
        public int Order { get; set; }

        [NotMapped]
        public List<Homes> TopGroups { get; set; }
        
        [NotMapped]
        public List<Homes> MyGroups { get; set; }
    }
}
