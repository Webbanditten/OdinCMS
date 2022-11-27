using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("rewards")]
    public class Rewards
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("item_definitions")]
        public string ItemDefinitions { get; set; }

        [Column("available_from")]
        public DateTime AvailableFrom { get; set; }

        [Column("available_to")]
        public DateTime AvailableTo { get; set; }
        
        [Column("description")]
        public string Description { get; set; }

        [NotMapped]
        public List<ItemsDefinitions> ItemsDefinitions { get; set; }
    }
}
