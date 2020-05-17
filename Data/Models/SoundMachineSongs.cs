using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("soundmachine_songs")]
    public class SoundMachineSongs
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("title")]
        public string Title { get; set; }
        [Column("data")]
        public string Data { get; set; }
    }
}
