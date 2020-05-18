using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("messenger_friends")]
    public class Friends
    {
        [Key]
        [Column("from_id")]
        public int FromId { get; set; }
        [Key]
        [Column("to_id")]
        public int ToId { get; set; }
        [Column("created")]
        public int Created { get; set; }

        [NotMapped]
        public string CreatedAsString
        {
            get
            {
                var friendsSinceDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                friendsSinceDate = friendsSinceDate.AddSeconds(Created).ToLocalTime();

                return friendsSinceDate.ToString("dd-MM-yyyy");
            }
        }
    }
}
