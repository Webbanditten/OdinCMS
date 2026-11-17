using System.Collections.Generic;
using System.Linq;
using KeplerCMS.Data.Models;

namespace KeplerCMS.Models
{
    /// <summary>One furni a catalogue item gives (a package gives several parts).</summary>
    public class CatalogueFurniPart
    {
        public ItemsDefinitions Definition { get; set; }
        public int SpecialSpriteId { get; set; }
        public int Amount { get; set; }
    }

    /// <summary>A catalogue item as the website gives or sells it.</summary>
    public class CatalogueFurniEntry
    {
        public int Id { get; set; }
        public string SaleCode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public bool IsPackage { get; set; }
        /// <summary>For a sale code: every catalogue row with it is hidden. For an item: this row is.</summary>
        public bool IsHidden { get; set; }
        public List<int> PageIds { get; set; } = new List<int>();
        public List<CatalogueFurniPart> Parts { get; set; } = new List<CatalogueFurniPart>();
        /// <summary>Why the website can't hand it out (null = it can): pets and decorations need a choice in the catalogue.</summary>
        public string NotGivable { get; set; }
        /// <summary>
        /// Any player can buy it in the game: not hidden, on a catalogue page everyone may open (fuse DEFAULT, not club
        /// only) that is in the catalogue index or linked from such a page.
        /// </summary>
        public bool IsPublic { get; set; }
        public ItemsDefinitions FirstDefinition => Parts.FirstOrDefault()?.Definition;
    }

    public class CatalogueFurniCatalogue
    {
        public Dictionary<int, CatalogueFurniEntry> ById { get; set; } = new Dictionary<int, CatalogueFurniEntry>();
        /// <summary>
        /// By sale code, as Odin resolves voucher items: a visible item wins over a hidden one. A sale code shared by
        /// different furni can't be given.
        /// </summary>
        public Dictionary<string, CatalogueFurniEntry> BySaleCode { get; set; } = new Dictionary<string, CatalogueFurniEntry>();
    }
}
