using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("cms_upload")]
    public class Upload
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("category")]
        public string Category { get; set; }
        [Column("blob")]
        public byte[] Blob { get; set; }
        [Column("content_type")]
        public string ContentType { get; set; }



    }
}
