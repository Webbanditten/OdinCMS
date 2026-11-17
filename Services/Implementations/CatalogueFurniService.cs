using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KeplerCMS.Services.Implementations
{
    public class CatalogueFurniService : ICatalogueFurniService
    {
        private const string RoomDimmerDefault = "1,1,1,#000000,255"; // Odin Item.DEFAULT_ROOMDIMMER_CUSTOM_DATA
        private readonly DataContext _context;
        private readonly ContextSql _sql;
        private CatalogueFurniCatalogue _loaded;

        public CatalogueFurniService(DataContext context)
        {
            _context = context;
            _sql = new ContextSql(context);
        }

        private static bool HasBehaviour(ItemsDefinitions definition, string behaviour) =>
            (definition?.Behavior ?? "").Split(',').Any(b => b.Trim().Equals(behaviour, StringComparison.OrdinalIgnoreCase));

        private static IEnumerable<int> PageIds(string pageIds) =>
            (pageIds ?? "").Split(',').Select(p => int.TryParse(p.Trim(), out var id) ? id : 0).Where(id => id > 0);

        /// <summary>
        /// The catalogue pages any player can open: fuse DEFAULT, not club only, and in the index or reachable through
        /// another such page's links (link_list: name_index values, e.g. Kamera → Kamera2).
        /// </summary>
        private static HashSet<int> PublicPages(List<CataloguePages> pages)
        {
            var open = pages.Where(p => string.Equals(p.Fuse ?? "DEFAULT", "DEFAULT", StringComparison.OrdinalIgnoreCase) && p.ClubOnly == 0).ToList();
            var byName = open.Where(p => !string.IsNullOrEmpty(p.NameIndex)).GroupBy(p => p.NameIndex, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var reached = new HashSet<int>();
            var queue = new Queue<CataloguePages>(open.Where(p => p.IndexVisible == 1));
            while (queue.Count > 0)
            {
                var page = queue.Dequeue();
                if (!reached.Add(page.Id)) continue;
                foreach (var link in (page.LinkList ?? "").Split(',').Select(l => l.Trim()).Where(l => l.Length > 0))
                {
                    if (byName.TryGetValue(link, out var linked) && !reached.Contains(linked.Id)) queue.Enqueue(linked);
                }
            }
            return reached;
        }

        public async Task<CatalogueFurniCatalogue> Load()
        {
            if (_loaded != null) return _loaded;

            var items = await _context.CatalogueItems.AsNoTracking().OrderBy(i => i.Order).ThenBy(i => i.Id).ToListAsync();
            var definitions = await _context.ItemsDefinitions.AsNoTracking().ToDictionaryAsync(d => d.Id);
            var packages = (await _context.CataloguePackages.AsNoTracking().OrderBy(p => p.Id).ToListAsync())
                .GroupBy(p => p.SaleCode ?? "", StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            var publicPages = PublicPages(await _context.CataloguePages.AsNoTracking().ToListAsync());
            var catalogue = new CatalogueFurniCatalogue();

            foreach (var item in items)
            {
                var entry = new CatalogueFurniEntry
                {
                    Id = item.Id,
                    SaleCode = item.SaleCode ?? "",
                    Price = item.Price,
                    IsPackage = item.IsPackage,
                    IsHidden = item.IsHidden,
                    PageIds = PageIds(item.PageId).ToList(),
                };

                if (item.IsPackage)
                {
                    foreach (var part in packages.GetValueOrDefault(entry.SaleCode) ?? new List<CataloguePackages>())
                    {
                        entry.Parts.Add(new CatalogueFurniPart { Definition = definitions.GetValueOrDefault(part.DefId ?? 0), SpecialSpriteId = part.SpriteId ?? 0, Amount = Math.Max(1, part.Amount ?? 1) });
                    }
                    entry.Name = item.PackageName;
                    entry.Description = item.PackageDescription;
                }
                else
                {
                    var definition = definitions.GetValueOrDefault(item.DefinitionId);
                    entry.Parts.Add(new CatalogueFurniPart { Definition = definition, SpecialSpriteId = item.ItemSpecialSpriteId, Amount = 1 });
                    // Odin names special-sprite items (posters) by the catalogue row, others by the furni.
                    var own = item.ItemSpecialSpriteId > 0 && !string.IsNullOrWhiteSpace(item.Name);
                    entry.Name = own ? item.Name : definition?.Name;
                    entry.Description = own ? item.Description : definition?.Description;
                }
                if (string.IsNullOrWhiteSpace(entry.Name)) entry.Name = entry.SaleCode; // e.g. film has no name

                if (entry.Parts.Count == 0 || entry.Parts.Any(p => p.Definition == null))
                    entry.NotGivable = "Its furni definition is missing";
                else if (entry.Parts.Any(p => HasBehaviour(p.Definition, "decoration")))
                    entry.NotGivable = "Wallpaper, floors and landscapes need a pattern picked in the catalogue";
                else if (entry.Parts.Any(p => string.Equals(p.Definition.Interactor, "pet_nest", StringComparison.OrdinalIgnoreCase)))
                    entry.NotGivable = "Pets need a name and race picked in the catalogue";

                entry.IsPublic = !item.IsHidden && entry.PageIds.Any(publicPages.Contains);
                catalogue.ById[entry.Id] = entry;
            }

            foreach (var group in catalogue.ById.Values.Where(e => e.SaleCode.Length > 0).GroupBy(e => e.SaleCode, StringComparer.Ordinal))
            {
                var first = group.FirstOrDefault(e => !e.IsHidden) ?? group.First();
                var bySaleCode = new CatalogueFurniEntry
                {
                    Id = first.Id, SaleCode = first.SaleCode, Name = first.Name, Description = first.Description, Price = first.Price,
                    IsPackage = first.IsPackage, IsHidden = group.All(e => e.IsHidden), PageIds = group.SelectMany(e => e.PageIds).Distinct().ToList(),
                    Parts = first.Parts, NotGivable = first.NotGivable, IsPublic = group.Any(e => e.IsPublic),
                };
                var kinds = group.Select(e => (e.IsPackage, e.Parts.FirstOrDefault()?.Definition?.Id, e.Parts.FirstOrDefault()?.SpecialSpriteId, e.Parts.Count)).Distinct().Count();
                if (kinds > 1) bySaleCode.NotGivable = "Several different catalogue items use this sale code";
                catalogue.BySaleCode[bySaleCode.SaleCode] = bySaleCode;
            }

            return _loaded = catalogue;
        }

        public async Task<int> Give(int userId, string username, CatalogueFurniPart part)
        {
            var definition = part.Definition;
            if (definition.Sprite == "film") return 5;

            var customData = "";
            if (!HasBehaviour(definition, "decoration") && part.SpecialSpriteId > 0) customData = part.SpecialSpriteId.ToString();
            if (HasBehaviour(definition, "post_it")) customData = "20";
            if (HasBehaviour(definition, "prize_trophy")) customData += username + "\t" + DateTime.Now.ToString("dd-MM-yyyy") + "\t";
            if (HasBehaviour(definition, "roomdimmer")) customData = RoomDimmerDefault;

            var itemId = await InsertItem(userId, definition.Id, customData);
            if (HasBehaviour(definition, "teleporter"))
            {
                var linkedId = await InsertItem(userId, definition.Id, customData);
                await _sql.Execute("INSERT INTO items_teleporter_links (item_id, linked_id) VALUES (@a, @b), (@b, @a)", ("@a", itemId), ("@b", linkedId));
            }

            return definition.Sprite == "camera" ? 2 : 0;
        }

        private async Task<long> InsertItem(int userId, int definitionId, string customData)
        {
            await _sql.Execute("INSERT INTO items (user_id, definition_id, custom_data, owned_since) VALUES (@user, @definition, @data, @since)",
                ("@user", userId), ("@definition", definitionId), ("@data", customData), ("@since", DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            return await _sql.LastInsertId();
        }
    }
}
