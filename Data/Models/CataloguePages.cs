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

        [Column("order_id")]
        public int OrderId { get; set; }
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
    }
}
