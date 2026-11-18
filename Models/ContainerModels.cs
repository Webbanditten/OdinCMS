using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Models;

public class PopularFurniContainer
{
    public string Name { get; set; }
    public string Sprite { get; set; }
    /// <summary>The sprite for the furni imager when that isn't Sprite (posters: "poster27").</summary>
    public string ImageSprite { get; set; }
    public int SpriteId { get; set; }
    public int Amount { get; set; }
    public int DefinitionId { get; set; }
}