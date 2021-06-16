using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("catalogue_packages")]
    public class CataloguePackages
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("salecode")]
        public string SaleCode { get; set; }

        [Column("definition_id")]
        public int DefId { get; set; }

        [Column("special_sprite_id")]
        public int SpriteId { get; set; }

        [Column("amount")]
        public int Amount { get; set; }
    }
}
