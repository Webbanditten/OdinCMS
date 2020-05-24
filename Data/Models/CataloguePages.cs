using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("catalogue_pages")]
    public class CataloguePages
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("min_role")]
        public int MinRole { get; set; }


    }
}
