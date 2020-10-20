using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("items_photos")]
    public class ItemsPhotos
    {
        [Column("photo_id")]
        public int Id { get; set; }
        [Column("photo_user_id")]
        public int UserId { get; set; }
        [Column("photo_data")]
        public byte[] PhotoData { get; set; }
    }
}
