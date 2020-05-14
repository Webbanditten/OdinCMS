using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("cms_homes")]
    public class Homes
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("allow_display")]
        public int IAllowDisplay { get; set; }
        [Column("type")]
        public string Type { get; set; }
        [Column("background")]
        public string Background { get; set; }

        [NotMapped]
        public bool AllowDisplay
        {
            get { return IAllowDisplay == 1; }
            set { IAllowDisplay = value ? 1 : 0; }
        }

    }

    [Table("cms_homes_catalog")]
    public class HomesCatalog
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("category")]
        public int Category { get; set; }
        [Column("item_id")]
        public int ItemId { get; set; }
        [Column("amount")]
        public int Amount { get; set; }
        [Column("price")]
        public int Price { get; set; }
    }

    [Table("cms_homes_categories")]
    public class HomesCategories
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("parent_id")]
        public int ParentId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("order")]
        public int Order { get; set; }
        [Column("type")]
        public string Type { get; set; }
        [Column("fuse")]
        public string Fuse { get; set; }
        [Column("enabled")]
        public int IEnabled { get; set; }
        [NotMapped]
        public bool Enabled
        {
            get { return IEnabled == 1; }
            set { IEnabled = value ? 1 : 0; }
        }


    }

    [Table("cms_homes_items")]
    public class HomesItems
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("home_id")]
        public int HomeId { get; set; }
        [Column("item_id")]
        public int ItemId { get; set; }
        [Column("x")]
        public int X { get; set; }
        [Column("Y")]
        public int Y { get; set; }
        [Column("z")]
        public int Z { get; set; }
        [Column("skin")]
        public string Skin { get; set; }
        [Column("data")]
        public string Data { get; set; }
        [Column("owner_id")]
        public int OwnerId { get; set; }


    }


    [Table("cms_homes_item_data")]
    public class HomesItemData
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("description")]
        public string Description { get; set; }
        [Column("type")]
        public string Type { get; set; }
        [Column("css_class")]
        public string CssClass { get; set; }


    }

    [Table("cms_homes_inventory")]
    public class HomesInventory
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("amount")]
        public int Amount { get; set; }
        [Column("item_id")]
        public int ItemId { get; set; }


    }

}
