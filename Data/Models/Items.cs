using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
    [Table("items")]
    public class Items
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("definition_id")]
        public int DefinitionId { get; set; }
        [Column("custom_data")]
        public string CustomData { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        public ItemsDefinitions Definition { get; set; }


    }
}
