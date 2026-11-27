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
    /// <summary>
    /// The in-game catalogue's data for housekeeping: pages (catalogue_pages), their products (catalogue_items, whose
    /// page_id may list several pages), packages (catalogue_packages, by sale code) and furni master data
    /// (items_definitions). Odin reads all of it at start and on the reload_catalogue command.
    /// </summary>
    public class CatalogueService : ICatalogueService
    {
        private readonly DataContext _context;

        public CatalogueService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<CataloguePages>> GetCataloguePages()
        {
            return await _context.CataloguePages.ToListAsync();
        }

        // ---- Helpers ----

        private static List<int> ParsePages(string pageId) =>
            (pageId ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => int.TryParse(p.Trim(), out var id) ? id : 0).Where(id => id > 0).Distinct().ToList();

        private static string FormatPages(IEnumerable<int> pages) => string.Join(",", pages.Distinct());

        /// <summary>label_extra_t as Odin reads it: "n:text" lines separated by CRLF.</summary>
        private static Dictionary<int, string> ParseTexts(string labelExtraT)
        {
            var texts = new Dictionary<int, string>();
            foreach (var line in (labelExtraT ?? "").Replace("\r\n", "\n").Split('\n'))
            {
                var colon = line.IndexOf(':');
                if (colon > 0 && int.TryParse(line[..colon].Trim(), out var number)) texts[number] = line[(colon + 1)..];
            }
            return texts;
        }

        private static string FormatTexts(Dictionary<int, string> texts) =>
            string.Join("\r\n", (texts ?? new()).Where(t => !string.IsNullOrEmpty(t.Value)).OrderBy(t => t.Key)
                .Select(t => t.Key + ":" + t.Value.Replace("\r\n", "<br>").Replace("\n", "<br>")));

        private static CatalogueFurni ToFurni(ItemsDefinitions d) => new()
        {
            Id = d.Id, Sprite = d.Sprite ?? "", SpriteId = d.SpriteId, Name = d.Name ?? "", Description = d.Description ?? "",
            Colour = d.Colour ?? "", Length = d.Length, Width = d.Width, TopHeight = d.TopHeight, MaxStatus = d.MaxStatus ?? "0",
            Behaviour = d.Behavior ?? "", Interactor = d.Interactor ?? "default", IsTradable = d.IsTradable == 1,
            IsRecyclable = d.IsRecyclable == 1, DrinkIds = d.DrinkIds ?? "",
        };

        private static CataloguePageEdit ToPage(CataloguePages p, int productCount) => new()
        {
            Id = p.Id, Order = p.OrderId ?? 0, NameIndex = p.NameIndex ?? "", Name = p.Name ?? "", Layout = p.Layout ?? "",
            Headline = p.ImageHeadline ?? "",
            Teasers = (p.ImageTeasers ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList(),
            Body = p.Body ?? "", LabelPick = p.LabelPick ?? "", LabelExtraS = p.LabelExtraS ?? "", Texts = ParseTexts(p.LabelExtraT),
            Links = (p.LinkList ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList(),
            IndexVisible = p.IndexVisible == 1, ClubOnly = p.ClubOnly == 1, Fuse = string.IsNullOrEmpty(p.Fuse) ? "DEFAULT" : p.Fuse,
            ProductCount = productCount,
            ParentId = p.ParentId, TreeOrder = p.TreeOrder, OrderId = p.OrderId ?? 0, Icon = p.Icon, Colour = p.Colour, LayoutV31 = p.LayoutV31 ?? "",
        };

        private static string Nullable(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        // ---- Pages ----

        public async Task<List<CataloguePageEdit>> GetPages()
        {
            var pages = await _context.CataloguePages.ToListAsync();
            var counts = (await _context.CatalogueItems.Select(i => i.PageId).ToListAsync())
                .SelectMany(ParsePages).GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
            return pages.OrderBy(p => p.OrderId ?? int.MaxValue).ThenBy(p => p.Id)
                .Select(p => ToPage(p, counts.GetValueOrDefault(p.Id))).ToList();
        }

        public async Task<CataloguePageEdit> GetPage(int id)
        {
            var page = await _context.CataloguePages.FirstOrDefaultAsync(p => p.Id == id);
            if (page == null) return null;
            var products = await GetProducts(id);
            return ToPage(page, products.Count);
        }

        public async Task<int> SavePage(CataloguePageEdit edit)
        {
            var page = edit.Id > 0 ? await _context.CataloguePages.FirstOrDefaultAsync(p => p.Id == edit.Id) : null;
            if (page == null)
            {
                // catalogue_pages.id isn't auto-increment.
                var nextId = (await _context.CataloguePages.MaxAsync(p => (int?)p.Id) ?? 0) + 1;
                var nextOrder = (await _context.CataloguePages.MaxAsync(p => p.OrderId) ?? 0) + 1;
                page = new CataloguePages { Id = nextId, OrderId = nextOrder };
                _context.CataloguePages.Add(page);
            }
            page.Name = edit.Name.Trim();
            page.NameIndex = string.IsNullOrWhiteSpace(edit.NameIndex) ? page.Name : edit.NameIndex.Trim();
            page.Layout = (edit.Layout ?? "").Trim();
            page.ParentId = edit.ParentId > 0 && edit.ParentId != page.Id ? edit.ParentId : -1;
            page.Icon = Math.Clamp(edit.Icon, 0, 99);
            page.Colour = Math.Clamp(edit.Colour, 0, 11);
            page.LayoutV31 = Nullable(edit.LayoutV31);
            // Odin always sends the headline (g) and fails on NULL: no image is an empty string.
            page.ImageHeadline = (edit.Headline ?? "").Trim();
            var teasers = (edit.Teasers ?? new()).Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList();
            page.ImageTeasers = teasers.Count == 0 ? null : string.Join(",", teasers) + ",";
            page.Body = (edit.Body ?? "").Replace("\r\n", "<br>").Replace("\n", "<br>");
            page.LabelPick = Nullable(edit.LabelPick?.Replace("\r\n", "<br>").Replace("\n", "<br>"));
            page.LabelExtraS = Nullable(edit.LabelExtraS?.Replace("\r\n", "<br>").Replace("\n", "<br>"));
            var texts = FormatTexts(edit.Texts);
            page.LabelExtraT = texts.Length == 0 ? null : texts;
            page.LinkList = string.Join(",", (edit.Links ?? new()).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()));
            page.IndexVisible = edit.IndexVisible ? 1 : 0;
            page.ClubOnly = edit.ClubOnly ? 1 : 0;
            page.Fuse = string.IsNullOrWhiteSpace(edit.Fuse) ? "DEFAULT" : edit.Fuse.Trim();
            await _context.SaveChangesAsync();
            return page.Id;
        }

        /// <summary>Delete a page. Its products leave it; products on no other page are deleted.</summary>
        public async Task DeletePage(int id)
        {
            var page = await _context.CataloguePages.FirstOrDefaultAsync(p => p.Id == id);
            if (page == null) return;
            var items = (await _context.CatalogueItems.Where(i => i.PageId.Contains(id.ToString())).ToListAsync())
                .Where(i => ParsePages(i.PageId).Contains(id)).ToList();
            foreach (var item in items)
            {
                var pages = ParsePages(item.PageId).Where(p => p != id).ToList();
                if (pages.Count == 0) _context.CatalogueItems.Remove(item);
                else item.PageId = FormatPages(pages);
            }
            // Children of a deleted folder move to its parent.
            foreach (var child in await _context.CataloguePages.Where(p => p.ParentId == id).ToListAsync()) child.ParentId = page.ParentId;
            _context.CataloguePages.Remove(page);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Which page images come from the image library rather than a cast (images.json lists the cast ones), for
        /// Odin to send to the v31 catalogue as name_language. Replaces catalogue_library_images.
        /// </summary>
        public async Task<int> SetLibraryImages(IEnumerable<string> names)
        {
            var list = names.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct().ToList();
            _context.CatalogueLibraryImages.RemoveRange(await _context.CatalogueLibraryImages.ToListAsync());
            _context.CatalogueLibraryImages.AddRange(list.Select(n => new CatalogueLibraryImage { Name = n }));
            await _context.SaveChangesAsync();
            return list.Count;
        }

        /// <summary>
        /// The v31 catalogue's tree as arranged in the editor: every page's parent and place among its siblings
        /// (tree_order, from 1). The classic catalogue's order (order_id) stays as it is. A parent that doesn't exist, the
        /// page itself or one of its own descendants puts the page at the top.
        /// </summary>
        public async Task SaveTree(List<CatalogueTreeNode> nodes)
        {
            var pages = await _context.CataloguePages.ToListAsync();
            var byId = pages.ToDictionary(p => p.Id);
            var parentOf = nodes.Where(n => byId.ContainsKey(n.Id)).GroupBy(n => n.Id).ToDictionary(g => g.Key, g => g.Last().ParentId > 0 && byId.ContainsKey(g.Last().ParentId) ? g.Last().ParentId : -1);
            foreach (var node in nodes)
            {
                if (!byId.TryGetValue(node.Id, out var page)) continue;
                // walk up from the new parent: reaching the page itself would make a loop
                var parent = parentOf[node.Id];
                var seen = new HashSet<int> { node.Id };
                for (var up = parent; up > 0; up = parentOf.TryGetValue(up, out var next) ? next : byId[up].ParentId)
                {
                    if (!seen.Add(up)) { parent = -1; break; }
                }
                page.ParentId = parent;
                page.TreeOrder = Math.Max(0, node.TreeOrder);
            }
            await _context.SaveChangesAsync();
        }

        public async Task ReorderPages(List<int> ids)
        {
            var pages = await _context.CataloguePages.ToListAsync();
            for (var i = 0; i < ids.Count; i++)
            {
                var page = pages.FirstOrDefault(p => p.Id == ids[i]);
                if (page != null) page.OrderId = i + 1;
            }
            await _context.SaveChangesAsync();
        }

        // ---- Products ----

        public async Task<List<CatalogueProductEdit>> GetProducts(int pageId)
        {
            var key = pageId.ToString();
            var items = (await _context.CatalogueItems.Where(i => i.PageId.Contains(key)).ToListAsync())
                .Where(i => ParsePages(i.PageId).Contains(pageId))
                .OrderBy(i => i.Order).ThenBy(i => i.Id).ToList();
            var saleCodes = items.Where(i => i.IsPackage).Select(i => i.SaleCode).ToList();
            var packages = await _context.CataloguePackages.Where(p => saleCodes.Contains(p.SaleCode)).ToListAsync();
            var definitionIds = items.Select(i => i.DefinitionId).Concat(packages.Select(p => p.DefId ?? 0)).Distinct().ToList();
            var definitions = await _context.ItemsDefinitions.Where(d => definitionIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id);

            return items.Select(item =>
            {
                var definition = definitions.TryGetValue(item.DefinitionId, out var d) ? ToFurni(d) : null;
                var ownTexts = item.IsPackage || item.ItemSpecialSpriteId > 0;
                return new CatalogueProductEdit
                {
                    Id = item.Id, PageIds = ParsePages(item.PageId), Order = item.Order, Price = item.Price, Amount = item.Amount,
                    Hidden = item.IsHidden, SaleCode = item.SaleCode ?? "", DefinitionId = item.DefinitionId,
                    SpecialSpriteId = item.ItemSpecialSpriteId, IsPackage = item.IsPackage,
                    PackageName = item.PackageName ?? "", PackageDescription = item.PackageDescription ?? "",
                    OwnName = item.Name ?? "", OwnDescription = item.Description ?? "",
                    Name = item.IsPackage ? item.PackageName ?? "" : ownTexts ? item.Name ?? "" : definition?.Name ?? "",
                    Description = item.IsPackage ? item.PackageDescription ?? "" : ownTexts ? item.Description ?? "" : definition?.Description ?? "",
                    Definition = definition,
                    Packages = packages.Where(p => p.SaleCode == item.SaleCode).OrderBy(p => p.Id).Select(p => new CataloguePackageEdit
                    {
                        DefinitionId = p.DefId ?? 0, Amount = p.Amount ?? 1, SpecialSpriteId = p.SpriteId ?? 0,
                        Definition = definitions.TryGetValue(p.DefId ?? 0, out var pd) ? ToFurni(pd) : null,
                    }).ToList(),
                };
            }).ToList();
        }

        public async Task<List<string>> DuplicateSaleCodes(IEnumerable<string> saleCodes)
        {
            var codes = saleCodes.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
            return await _context.CatalogueItems.Where(i => codes.Contains(i.SaleCode))
                .GroupBy(i => i.SaleCode).Where(g => g.Count() > 1).Select(g => g.Key).ToListAsync();
        }

        private async Task<string> UniqueSaleCode(string wanted, int exceptId = 0)
        {
            var code = string.IsNullOrWhiteSpace(wanted) ? "item" : wanted.Trim();
            var taken = await _context.CatalogueItems.Where(i => i.Id != exceptId && i.SaleCode.StartsWith(code)).Select(i => i.SaleCode).ToListAsync();
            if (!taken.Contains(code)) return code;
            for (var n = 2; ; n++)
            {
                if (!taken.Contains(code + "_" + n)) return code + "_" + n;
            }
        }

        public async Task<int> SaveProduct(CatalogueProductEdit edit)
        {
            var item = await _context.CatalogueItems.FirstOrDefaultAsync(i => i.Id == edit.Id);
            if (item == null) return 0;
            item.Price = Math.Max(0, edit.Price);
            item.Amount = Math.Max(1, edit.Amount);
            item.IsHidden = edit.Hidden;
            if (!string.IsNullOrWhiteSpace(edit.SaleCode)) item.SaleCode = edit.SaleCode.Trim();
            if (edit.PageIds != null && edit.PageIds.Count > 0) item.PageId = FormatPages(edit.PageIds);
            item.ItemSpecialSpriteId = Math.Max(0, edit.SpecialSpriteId);
            item.IsPackage = edit.IsPackage;
            item.PackageName = Nullable(edit.PackageName);
            item.PackageDescription = Nullable(edit.PackageDescription);
            item.Name = edit.OwnName ?? "";
            item.Description = edit.OwnDescription ?? "";
            if (edit.DefinitionId > 0) item.DefinitionId = edit.DefinitionId;

            if (item.IsPackage)
            {
                var old = await _context.CataloguePackages.Where(p => p.SaleCode == item.SaleCode).ToListAsync();
                _context.CataloguePackages.RemoveRange(old);
                foreach (var part in (edit.Packages ?? new()).Where(p => p.DefinitionId > 0))
                {
                    _context.CataloguePackages.Add(new CataloguePackages
                    {
                        SaleCode = item.SaleCode, DefId = part.DefinitionId, Amount = Math.Max(1, part.Amount), SpriteId = part.SpecialSpriteId,
                    });
                }
            }
            await _context.SaveChangesAsync();
            return item.Id;
        }

        /// <summary>Put furni on a page as new products (at the end, 3 credits, sale code from the sprite).</summary>
        public async Task<List<int>> AddProducts(int pageId, List<int> definitionIds)
        {
            var definitions = await _context.ItemsDefinitions.Where(d => definitionIds.Contains(d.Id)).ToListAsync();
            var key = pageId.ToString();
            var order = (await _context.CatalogueItems.Where(i => i.PageId.Contains(key)).ToListAsync())
                .Where(i => ParsePages(i.PageId).Contains(pageId)).Select(i => i.Order).DefaultIfEmpty(0).Max() + 1;
            var added = new List<CatalogueItems>();
            foreach (var definition in definitionIds.Select(id => definitions.FirstOrDefault(d => d.Id == id)).Where(d => d != null))
            {
                var item = new CatalogueItems
                {
                    SaleCode = await UniqueSaleCode(definition.Sprite),
                    PageId = key, Order = order++, Price = 3, Amount = 1, DefinitionId = definition.Id,
                    Name = definition.Name ?? "", Description = definition.Description ?? "",
                };
                _context.CatalogueItems.Add(item);
                await _context.SaveChangesAsync(); // one at a time, so the next sale code sees this one
                added.Add(item);
            }
            return added.Select(i => i.Id).ToList();
        }

        /// <summary>Take a product off a page; it is deleted when no page is left.</summary>
        public async Task RemoveProduct(int id, int pageId)
        {
            var item = await _context.CatalogueItems.FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return;
            var pages = ParsePages(item.PageId).Where(p => p != pageId).ToList();
            if (pages.Count == 0) _context.CatalogueItems.Remove(item);
            else item.PageId = FormatPages(pages);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Odin shows a page's products by order_id, then id. A product's order_id is shared by every page it is on, so
        /// the page's own order_id values are handed out again in the new order (other pages keep their relative order).
        /// </summary>
        public async Task ReorderProducts(int pageId, List<int> ids)
        {
            var items = await _context.CatalogueItems.Where(i => ids.Contains(i.Id)).ToListAsync();
            var slots = items.OrderBy(i => i.Order).ThenBy(i => i.Id).Select(i => i.Order).ToList();
            if (slots.Distinct().Count() < slots.Count)
            {
                // Ties (e.g. all 0): give the page its own distinct run.
                var start = slots.Count > 0 ? slots.Min() : 0;
                slots = Enumerable.Range(start, slots.Count).ToList();
            }
            var ordered = ids.Select(id => items.FirstOrDefault(x => x.Id == id)).Where(x => x != null).ToList();
            for (var i = 0; i < ordered.Count; i++) ordered[i].Order = slots[i];
            await _context.SaveChangesAsync();
        }

        // ---- Furni master data ----

        public async Task<List<CatalogueFurni>> SearchFurni(string query, int limit = 80)
        {
            query = (query ?? "").Trim();
            if (query.Length == 0) return new List<CatalogueFurni>();
            int.TryParse(query, out var id);
            var found = await _context.ItemsDefinitions
                .Where(d => d.Id == id || d.Sprite.Contains(query) || d.Name.Contains(query))
                .OrderBy(d => d.Sprite == query || d.Id == id ? 0 : d.Sprite.StartsWith(query) || d.Name.StartsWith(query) ? 1 : 2)
                .ThenBy(d => d.Name).Take(limit).ToListAsync();
            var ids = found.Select(d => d.Id).ToList();
            var onPages = (await _context.CatalogueItems.Where(i => ids.Contains(i.DefinitionId)).Select(i => new { i.DefinitionId, i.PageId }).ToListAsync())
                .GroupBy(i => i.DefinitionId).ToDictionary(g => g.Key, g => g.SelectMany(i => ParsePages(i.PageId)).Distinct().ToList());
            return found.Select(d =>
            {
                var furni = ToFurni(d);
                furni.OnPages = onPages.GetValueOrDefault(d.Id) ?? new List<int>();
                return furni;
            }).ToList();
        }

        public async Task<CatalogueFurni> GetFurni(int id)
        {
            var definition = await _context.ItemsDefinitions.FirstOrDefaultAsync(d => d.Id == id);
            return definition == null ? null : ToFurni(definition);
        }

        public async Task<int> MaxSpriteId() => await _context.ItemsDefinitions.MaxAsync(d => (int?)d.SpriteId) ?? 0;

        public async Task<bool> SpriteExists(string sprite, int exceptId) =>
            await _context.ItemsDefinitions.AnyAsync(d => d.Sprite == sprite && d.Id != exceptId);

        public async Task<int> SaveFurni(CatalogueFurni edit)
        {
            var definition = edit.Id > 0 ? await _context.ItemsDefinitions.FirstOrDefaultAsync(d => d.Id == edit.Id) : null;
            if (definition == null)
            {
                definition = new ItemsDefinitions();
                _context.ItemsDefinitions.Add(definition);
            }
            definition.Sprite = edit.Sprite.Trim();
            definition.SpriteId = edit.SpriteId;
            definition.Name = (edit.Name ?? "").Trim();
            definition.Description = (edit.Description ?? "").Trim();
            definition.Colour = string.IsNullOrWhiteSpace(edit.Colour) ? "0,0,0" : edit.Colour.Trim();
            definition.Length = Math.Max(0, edit.Length);
            definition.Width = Math.Max(0, edit.Width);
            definition.TopHeight = Math.Max(0, edit.TopHeight);
            definition.MaxStatus = string.IsNullOrWhiteSpace(edit.MaxStatus) ? "0" : edit.MaxStatus.Trim();
            definition.Behavior = string.Join(",", (edit.Behaviour ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(b => b.Trim().ToLowerInvariant()).Where(b => CatalogueFurniOptions.Behaviours.Contains(b)).Distinct());
            definition.Interactor = CatalogueFurniOptions.Interactors.Contains((edit.Interactor ?? "").Trim().ToLowerInvariant())
                ? edit.Interactor.Trim().ToLowerInvariant() : "default";
            definition.IsTradable = edit.IsTradable ? 1 : 0;
            definition.IsRecyclable = edit.IsRecyclable ? 1 : 0;
            definition.DrinkIds = (edit.DrinkIds ?? "").Trim();
            await _context.SaveChangesAsync();
            return definition.Id;
        }
    }
}
