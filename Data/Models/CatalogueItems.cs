using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("catalogue_items")]
    public class CatalogueItems
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("page_id")]
        public int PageId { get; set; }
        [Column("price")]
        public int Price { get; set; }
        [Column("definition_id")]
        public int DefinitionId { get; set; }
    }
}
