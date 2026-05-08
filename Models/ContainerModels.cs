using System.Collections.Generic;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Models;

public class PopularFurniContainer
{
    public string Name { get; set; }
    public string Sprite { get; set; }
    public int SpriteId { get; set; }
    public int Amount { get; set; }
    public int DefinitionId { get; set; }
}