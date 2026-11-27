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

        /// <summary>Deprecated: Odin ignores it (access is the page's fuse).</summary>
        [Column("min_role")]
        public int? MinRole { get; set; }

        [Column("order_id")]
        public int? OrderId { get; set; }
        [Column("index_visible")]
        public int IndexVisible { get; set; }
        [Column("is_club_only")]
        public int ClubOnly { get; set; }
        [Column("name_index")]
        public string NameIndex { get; set; }
        [Column("link_list")]
        public string LinkList { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("layout")]
        public string Layout { get; set; }
        [Column("image_headline")]
        public string ImageHeadline { get; set; }
        [Column("image_teasers")]
        public string ImageTeasers { get; set; }
        [Column("body")]
        public string Body { get; set; }
        [Column("label_pick")]
        public string LabelPick { get; set; }
        [Column("label_extra_s")]
        public string LabelExtraS { get; set; } 
        [Column("label_extra_t")]
        public string LabelExtraT { get; set; }
        [Column("fuse")]
        public string Fuse { get; set; }

        // The v31 catalogue's tree (Odin update.1.40.sql): parent page (-1 = top), node icon and colour, and the
        // layout it uses when not derived from Layout. A page without a layout is a folder.
        [Column("parent_id")]
        public int ParentId { get; set; } = -1;
        [Column("icon")]
        public int Icon { get; set; }
        [Column("colour")]
        public int Colour { get; set; }
        [Column("layout_v31")]
        public string LayoutV31 { get; set; }
    }

    /// <summary>Page images that come from the image library (Odin sends them to the v31 catalogue as name_language).</summary>
    [Table("catalogue_library_images")]
    public class CatalogueLibraryImage
    {
        [System.ComponentModel.DataAnnotations.Key]
        [Column("name")]
        public string Name { get; set; }
    }
}
