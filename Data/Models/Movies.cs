using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("cms_habbowood_movies")]
    public class Movies
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("data")]
        public string Data { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("session_id")]
        public string SessionId { get; set; }
        [Column("published")]
        public int IPublished { get; set; }

        [NotMapped]
        public bool Published
        {
            get { return IPublished == 1; }
            set { IPublished = value ? 1 : 0; }
        }
    }
}
