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
        [Column("sale_code")]
        public string SaleCode { get; set; }
        [Column("page_id")]
        public string PageId { get; set; }
        [Column("price")]
        public int Price { get; set; }
        [Column("order_id")]
        public int Order { get; set; }
        [Column("amount")]
        public int Amount { get; set; }
        [Column("definition_id")]
        public int DefinitionId { get; set; } 
        [Column("item_specialspriteid")]
        public int ItemSpecialSpriteId { get; set; }
        [Column("is_package")]
        public int IIsPackage { get; set; }

        [Column("is_hidden")]
        public int IIsHidden { get; set; }
        
        [Column("package_name")]
        public string PackageName { get; set; }
        [Column("package_description")]
        public string PackageDescription { get; set; }
        
        [NotMapped]
        public bool IsHidden
        {
            get { return IIsHidden == 1; }
            set { IIsHidden = value ? 1 : 0; }
        }

        [NotMapped]
        public bool IsPackage
        {
            get { return IIsPackage == 1; }
            set { IIsPackage = value ? 1 : 0; }
        }

    }
}
