using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeplerCMS.Data.Models
{
    /// <summary>A room layout (Odin's rooms_models, read only here): the floor plan rows split by "|", x = no floor, a digit
    /// = the floor's height; the door tile.</summary>
    [Table("rooms_models")]
    public class RoomModel
    {
        [Key][Column("id")] public int Id { get; set; }
        [Column("model_id")] public string ModelId { get; set; }
        [Column("door_x")] public int DoorX { get; set; }
        [Column("door_y")] public int DoorY { get; set; }
        [Column("door_z")] public double DoorZ { get; set; }
        [Column("heightmap")] public string Heightmap { get; set; }
    }

    /// <summary>A public room's own furniture (Odin's public_items, read only here): what stands on which tile.</summary>
    [Table("public_items")]
    public class PublicItem
    {
        [Key][Column("id")] public int Id { get; set; }
        [Column("room_model")] public string RoomModel { get; set; }
        [Column("sprite")] public string Sprite { get; set; }
        [Column("x")] public int X { get; set; }
        [Column("y")] public int Y { get; set; }
        [Column("rotation")] public int Rotation { get; set; }
        [Column("length")] public int Length { get; set; }
        [Column("width")] public int Width { get; set; }
        [Column("behaviour")] public string Behaviour { get; set; }
    }
}
