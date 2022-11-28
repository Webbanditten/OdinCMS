using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Data.Models
{
  [Table("items_definitions")]
    public class ItemsDefinitions
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("sprite")]
        public string sprite { get; set; }
        [Column("sprite_id")]
        public int SpriteId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("Description")]
        public string Description { get; set; }
        [Column("colour")]
        public string Colour { get; set; }
        [Column("length")]
        public int Length { get; set; }
        [Column("width")]
        public int Width { get; set; }
        [Column("top_height")]
        public int TopHeight { get; set; }
        [Column("max_status")]
        public string MaxStatus { get; set; }
        [Column("behaviour")]
        public string Behavior { get; set; }
        [Column("interactor")]
        public string Interactor { get; set; }
        [Column("is_tradable")]
        public int IsTradable { get; set; }
        [Column("is_recyclable")]
        public int IsRecyclable { get; set; }
        [Column("drink_ids")]
        public string DrinkIds { get; set; }
    }
}
