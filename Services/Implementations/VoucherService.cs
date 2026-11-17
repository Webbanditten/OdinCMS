using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace KeplerCMS.Services.Implementations
{
    /// <summary>
    /// Vouchers: <c>vouchers</c> (code, credits, expiry, single use), <c>vouchers_items</c> (one catalogue sale code per
    /// furni, repeated for several) and <c>vouchers_history</c> (who redeemed what). Odin redeems them in the game
    /// (purse), this service in housekeeping and on the website. The voucher tables are read and written with SQL:
    /// vouchers_items has no key, and the website redemption needs the same row lock Odin takes.
    /// </summary>
    public class VoucherService : IVoucherService
    {
        private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const string RoomDimmerDefault = "1,1,1,#000000,255"; // Odin Item.DEFAULT_ROOMDIMMER_CUSTOM_DATA
        public const int MaxCodes = 500;
        public const int MaxItems = 100;
        private static readonly Regex CodePattern = new Regex("^[A-Za-z0-9_-]{3,100}$");

        private readonly DataContext _context;
        private readonly ICommandQueueService _commandQueue;
        private readonly ILogger<VoucherService> _logger;

        public VoucherService(DataContext context, ICommandQueueService commandQueue, ILogger<VoucherService> logger)
        {
            _context = context;
            _commandQueue = commandQueue;
            _logger = logger;
        }

        // ---- What a sale code gives ----

        /// <summary>One furni a catalogue item gives (packages give several).</summary>
        private class Part
        {
            public Data.Models.ItemsDefinitions Definition;
            public int SpecialSpriteId;
            public int Amount;
        }

        private class CatalogueEntry
        {
            public string SaleCode;
            public string Name;
            public int Price;
            public bool IsPackage;
            public bool IsHidden;
            public List<Part> Parts = new List<Part>();
            public string NotAllowed;
        }

        private static bool HasBehaviour(Data.Models.ItemsDefinitions definition, string behaviour) =>
            (definition?.Behavior ?? "").Split(',').Any(b => b.Trim().Equals(behaviour, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Every sale code in the catalogue with the furni it gives. Like Odin's CatalogueManager.getVoucherItem, a visible
        /// item wins over a hidden one, and hidden items (not for sale) can be given too.
        /// </summary>
        private async Task<Dictionary<string, CatalogueEntry>> LoadCatalogue()
        {
            var items = await _context.CatalogueItems.AsNoTracking().OrderBy(i => i.Order).ThenBy(i => i.Id).ToListAsync();
            var definitions = await _context.ItemsDefinitions.AsNoTracking().ToDictionaryAsync(d => d.Id);
            var packages = (await _context.CataloguePackages.AsNoTracking().OrderBy(p => p.Id).ToListAsync())
                .GroupBy(p => p.SaleCode ?? "", StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            var entries = new Dictionary<string, CatalogueEntry>(StringComparer.Ordinal);

            foreach (var group in items.Where(i => !string.IsNullOrEmpty(i.SaleCode)).GroupBy(i => i.SaleCode, StringComparer.Ordinal))
            {
                var item = group.FirstOrDefault(i => !i.IsHidden) ?? group.First();
                var entry = new CatalogueEntry
                {
                    SaleCode = item.SaleCode,
                    Price = item.Price,
                    IsPackage = item.IsPackage,
                    IsHidden = group.All(i => i.IsHidden),
                };

                if (item.IsPackage)
                {
                    foreach (var part in packages.GetValueOrDefault(item.SaleCode) ?? new List<Data.Models.CataloguePackages>())
                    {
                        entry.Parts.Add(new Part { Definition = definitions.GetValueOrDefault(part.DefId ?? 0), SpecialSpriteId = part.SpriteId ?? 0, Amount = Math.Max(1, part.Amount ?? 1) });
                    }
                    entry.Name = string.IsNullOrWhiteSpace(item.PackageName) ? item.SaleCode : item.PackageName;
                }
                else
                {
                    var definition = definitions.GetValueOrDefault(item.DefinitionId);
                    entry.Parts.Add(new Part { Definition = definition, SpecialSpriteId = item.ItemSpecialSpriteId, Amount = 1 });
                    entry.Name = !string.IsNullOrWhiteSpace(item.Name) && item.ItemSpecialSpriteId > 0 ? item.Name : definition?.Name ?? item.SaleCode;
                }

                if (string.IsNullOrWhiteSpace(entry.Name)) entry.Name = item.SaleCode; // e.g. film has no name

                if (group.Select(i => (i.IsPackage, i.DefinitionId, i.ItemSpecialSpriteId)).Distinct().Count() > 1)
                    entry.NotAllowed = "Several different catalogue items use this sale code";
                else if (entry.Parts.Count == 0 || entry.Parts.Any(p => p.Definition == null))
                    entry.NotAllowed = "Its furni definition is missing";
                else if (entry.Parts.Any(p => HasBehaviour(p.Definition, "decoration")))
                    entry.NotAllowed = "Wallpaper, floors and landscapes need a pattern picked in the catalogue";
                else if (entry.Parts.Any(p => string.Equals(p.Definition.Interactor, "pet_nest", StringComparison.OrdinalIgnoreCase)))
                    entry.NotAllowed = "Pets need a name and race picked in the catalogue";

                entries[entry.SaleCode] = entry;
            }

            return entries;
        }

        // ---- SQL helpers (on EF's connection, inside its transaction when there is one) ----

        private DbCommand Command(string sql, params (string Name, object Value)[] parameters)
        {
            var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
            return command;
        }

        private async Task<int> Execute(string sql, params (string, object)[] parameters)
        {
            await using var command = Command(sql, parameters);
            return await command.ExecuteNonQueryAsync();
        }

        private async Task<List<T>> Query<T>(string sql, Func<DbDataReader, T> read, params (string, object)[] parameters)
        {
            var rows = new List<T>();
            await using var command = Command(sql, parameters);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) rows.Add(read(reader));
            return rows;
        }

        private async Task<T> InConnection<T>(Func<Task<T>> work)
        {
            await _context.Database.OpenConnectionAsync();
            try { return await work(); }
            finally { await _context.Database.CloseConnectionAsync(); }
        }

        // ---- Housekeeping ----

        public async Task<List<VoucherView>> GetVouchers() => await LoadVouchers(null);

        public async Task<VoucherView> GetVoucher(string code) => (await LoadVouchers(code)).FirstOrDefault();

        private async Task<List<VoucherView>> LoadVouchers(string code)
        {
            var catalogue = await LoadCatalogue();
            return await InConnection(async () =>
            {
                var filter = code == null ? "" : " WHERE voucher_code = @code";
                var vouchers = await Query("SELECT voucher_code, credits, expiry_date, is_single_use FROM vouchers" + filter + " ORDER BY voucher_code",
                    r => new VoucherView
                    {
                        Code = r.GetString(0),
                        Credits = r.GetInt32(1),
                        ExpiryDate = r.IsDBNull(2) ? null : r.GetDateTime(2),
                        IsSingleUse = Convert.ToInt32(r.GetValue(3)) == 1,
                    }, ("@code", code));
                var items = await Query("SELECT voucher_code, catalogue_sale_code FROM vouchers_items" + filter,
                    r => (Code: r.GetString(0), SaleCode: r.GetString(1)), ("@code", code));
                var counts = await Query("SELECT voucher_code, COUNT(*) FROM vouchers_history" + filter + " GROUP BY voucher_code",
                    r => (Code: r.GetString(0), Count: Convert.ToInt32(r.GetValue(1))), ("@code", code));
                var byCode = vouchers.ToDictionary(v => v.Code, StringComparer.OrdinalIgnoreCase);

                foreach (var group in items.GroupBy(i => (i.Code.ToLowerInvariant(), i.SaleCode)))
                {
                    if (!byCode.TryGetValue(group.First().Code, out var voucher)) continue;
                    var entry = catalogue.GetValueOrDefault(group.Key.SaleCode);
                    var definition = entry?.Parts.FirstOrDefault()?.Definition;
                    voucher.Furni.Add(new VoucherFurniLine
                    {
                        SaleCode = group.Key.SaleCode,
                        Amount = group.Count(),
                        Name = entry?.Name ?? group.Key.SaleCode,
                        Sprite = definition?.Sprite,
                        Colour = definition?.Colour,
                        Missing = entry == null,
                    });
                }
                foreach (var (voucherCode, count) in counts)
                {
                    if (byCode.TryGetValue(voucherCode, out var voucher)) voucher.Redemptions = count;
                }

                return vouchers.OrderBy(v => v.IsExpired).ThenByDescending(v => v.ExpiryDate ?? DateTime.MaxValue).ThenBy(v => v.Code).ToList();
            });
        }

        public async Task<List<VoucherFurni>> SearchFurni(string query)
        {
            query = (query ?? "").Trim();
            if (query.Length < 2) return new List<VoucherFurni>();
            var catalogue = await LoadCatalogue();
            return catalogue.Values
                .Where(e => e.SaleCode.Contains(query, StringComparison.OrdinalIgnoreCase) || (e.Name ?? "").Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.NotAllowed != null).ThenBy(e => e.IsHidden).ThenBy(e => e.Name)
                .Take(60)
                .Select(e => new VoucherFurni
                {
                    SaleCode = e.SaleCode,
                    Name = e.Name,
                    Sprite = e.Parts.FirstOrDefault()?.Definition?.Sprite,
                    Colour = e.Parts.FirstOrDefault()?.Definition?.Colour,
                    Price = e.Price,
                    IsPackage = e.IsPackage,
                    IsHidden = e.IsHidden,
                    NotAllowed = e.NotAllowed,
                })
                .ToList();
        }

        private static string NewCode()
        {
            var chars = new char[14];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = i == 4 || i == 9 ? '-' : CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
            }
            return new string(chars);
        }

        public async Task<VoucherSaveResult> Save(VoucherSaveRequest request, string existingCode = null)
        {
            var fail = new Func<string, VoucherSaveResult>(error => new VoucherSaveResult { Success = false, Error = error });
            if (request == null) return fail("Nothing to save.");
            var furni = (request.Furni ?? new List<VoucherFurniLine>()).Where(f => !string.IsNullOrWhiteSpace(f.SaleCode)).ToList();
            if (request.Credits < 0 || request.Credits > 1000000) return fail("Credits must be between 0 and 1,000,000.");
            if (furni.Any(f => f.Amount < 1 || f.Amount > MaxItems)) return fail($"A furni amount must be between 1 and {MaxItems}.");
            if (furni.Sum(f => f.Amount) > MaxItems) return fail($"A voucher can give at most {MaxItems} furni.");
            if (request.Credits == 0 && furni.Count == 0) return fail("Give credits, furni or both.");

            var catalogue = await LoadCatalogue();
            foreach (var line in furni)
            {
                if (!catalogue.TryGetValue(line.SaleCode, out var entry)) return fail($"No catalogue item has the sale code \"{line.SaleCode}\".");
                if (entry.NotAllowed != null) return fail($"\"{entry.Name}\" can't be given by a voucher: {entry.NotAllowed.ToLowerInvariant()}.");
            }

            DateTime? expiry = null;
            if (!string.IsNullOrWhiteSpace(request.Expiry))
            {
                if (!DateTime.TryParseExact(request.Expiry.Trim(), new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    return fail("The expiry date isn't a date (yyyy-MM-dd HH:mm).");
                expiry = parsed;
            }

            var codes = new List<string>();
            if (existingCode == null)
            {
                var count = Math.Max(1, request.Count);
                if (count > MaxCodes) return fail($"Create at most {MaxCodes} codes at once.");
                var code = (request.Code ?? "").Trim();
                if (code.Length > 0)
                {
                    if (count > 1) return fail("Several codes are made up at random: leave the code empty.");
                    if (!CodePattern.IsMatch(code)) return fail("A code is 3 to 100 letters, digits, - or _.");
                    codes.Add(code);
                }
            }

            return await InConnection(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                if (existingCode != null)
                {
                    var updated = await Execute("UPDATE vouchers SET credits = @credits, expiry_date = @expiry, is_single_use = @single WHERE voucher_code = @code",
                        ("@credits", request.Credits), ("@expiry", expiry), ("@single", request.SingleUse ? 1 : 0), ("@code", existingCode));
                    if (updated == 0)
                    {
                        // MariaDB reports 0 for an UPDATE that changes nothing: tell that apart from a missing voucher.
                        var exists = await Query("SELECT 1 FROM vouchers WHERE voucher_code = @code", r => 1, ("@code", existingCode));
                        if (exists.Count == 0) return fail("That voucher doesn't exist any more (redeemed or deleted).");
                    }
                    await Execute("DELETE FROM vouchers_items WHERE voucher_code = @code", ("@code", existingCode));
                    codes.Add(existingCode);
                }
                else
                {
                    if (codes.Count == 1)
                    {
                        var taken = await Query("SELECT 1 FROM vouchers WHERE voucher_code = @code", r => 1, ("@code", codes[0]));
                        if (taken.Count > 0) return fail($"The code \"{codes[0]}\" already exists.");
                    }
                    else
                    {
                        var wanted = Math.Max(1, request.Count);
                        var tries = 0;
                        while (codes.Count < wanted && tries++ < wanted * 10)
                        {
                            var candidate = NewCode();
                            if (codes.Contains(candidate)) continue;
                            var taken = await Query("SELECT 1 FROM vouchers WHERE voucher_code = @code", r => 1, ("@code", candidate));
                            if (taken.Count == 0) codes.Add(candidate);
                        }
                    }

                    foreach (var code in codes)
                    {
                        await Execute("INSERT INTO vouchers (voucher_code, credits, expiry_date, is_single_use) VALUES (@code, @credits, @expiry, @single)",
                            ("@code", code), ("@credits", request.Credits), ("@expiry", expiry), ("@single", request.SingleUse ? 1 : 0));
                    }
                }

                foreach (var code in codes)
                {
                    foreach (var line in furni)
                    {
                        for (var i = 0; i < line.Amount; i++)
                        {
                            await Execute("INSERT INTO vouchers_items (voucher_code, catalogue_sale_code) VALUES (@code, @sale)", ("@code", code), ("@sale", line.SaleCode));
                        }
                    }
                }

                await transaction.CommitAsync();
                return new VoucherSaveResult { Success = true, Codes = codes };
            });
        }

        public async Task<bool> Delete(string code)
        {
            return await InConnection(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var deleted = await Execute("DELETE FROM vouchers WHERE voucher_code = @code", ("@code", code));
                await Execute("DELETE FROM vouchers_items WHERE voucher_code = @code", ("@code", code));
                await transaction.CommitAsync();
                return deleted > 0;
            });
        }

        public async Task<List<VoucherHistoryView>> GetHistory(string code = null, int limit = 200)
        {
            return await InConnection(() => Query(
                "SELECT h.voucher_code, h.user_id, u.username, h.used_at, h.credits_redeemed, h.items_redeemed FROM vouchers_history h " +
                "LEFT JOIN users u ON u.id = h.user_id" + (code == null ? "" : " WHERE h.voucher_code = @code") + " ORDER BY h.used_at DESC LIMIT @limit",
                r => new VoucherHistoryView
                {
                    Code = r.GetString(0),
                    UserId = r.GetInt32(1),
                    Username = r.IsDBNull(2) ? null : r.GetString(2),
                    UsedAt = r.GetDateTime(3),
                    Credits = r.IsDBNull(4) ? null : r.GetInt32(4),
                    Items = r.IsDBNull(5) ? null : r.GetString(5),
                }, ("@code", code), ("@limit", limit)));
        }

        // ---- Website redemption ----

        public async Task<VoucherRedeemResult> Redeem(string code, int userId)
        {
            var result = new VoucherRedeemResult();
            code = (code ?? "").Trim();
            if (code.Length == 0 || code.Length > 100) return result;
            var catalogue = await LoadCatalogue();

            var redeemed = await InConnection(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                // The same claim as Odin's PurseDao.redeemVoucher: lock the voucher row, check, delete or log, commit.
                var vouchers = await Query("SELECT voucher_code, credits, is_single_use FROM vouchers WHERE voucher_code = @code " +
                    "AND (expiry_date IS NULL OR expiry_date > NOW()) FOR UPDATE",
                    r => (Code: r.GetString(0), Credits: r.GetInt32(1), SingleUse: Convert.ToInt32(r.GetValue(2)) == 1), ("@code", code));
                if (vouchers.Count == 0) return false;
                var voucher = vouchers[0];

                if (!voucher.SingleUse)
                {
                    var used = await Query("SELECT 1 FROM vouchers_history WHERE voucher_code = @code AND user_id = @user LIMIT 1", r => 1, ("@code", voucher.Code), ("@user", userId));
                    if (used.Count > 0) return false;
                }

                var saleCodes = await Query("SELECT catalogue_sale_code FROM vouchers_items WHERE voucher_code = @code", r => r.GetString(0), ("@code", voucher.Code));
                var username = (await Query("SELECT username FROM users WHERE id = @user", r => r.GetString(0), ("@user", userId))).FirstOrDefault();
                if (username == null) return false;

                if (voucher.SingleUse)
                {
                    await Execute("DELETE FROM vouchers WHERE voucher_code = @code", ("@code", voucher.Code));
                    await Execute("DELETE FROM vouchers_items WHERE voucher_code = @code", ("@code", voucher.Code));
                }

                if (voucher.Credits > 0)
                {
                    await Execute("UPDATE users SET credits = credits + @credits WHERE id = @user", ("@credits", voucher.Credits), ("@user", userId));
                    result.Credits = voucher.Credits;
                }

                var film = 0;
                foreach (var saleCode in saleCodes)
                {
                    if (!catalogue.TryGetValue(saleCode, out var entry) || entry.NotAllowed != null)
                    {
                        _logger.LogError("Voucher {Code}: can't give sale code {SaleCode} ({Reason})", voucher.Code, saleCode, entry?.NotAllowed ?? "no such catalogue item");
                        continue;
                    }
                    foreach (var part in entry.Parts)
                    {
                        for (var i = 0; i < part.Amount; i++)
                        {
                            film += await GiveFurni(userId, username, part);
                        }
                    }
                    result.Furni.Add(entry.Name);
                }

                if (film > 0)
                {
                    await Execute("UPDATE users SET film = film + @film WHERE id = @user", ("@film", film), ("@user", userId));
                }

                var counts = saleCodes.GroupBy(s => s, StringComparer.Ordinal).Select(g => g.Count() + "," + g.Key).ToList();
                await Execute("INSERT INTO vouchers_history (voucher_code, user_id, credits_redeemed, items_redeemed) VALUES (@code, @user, @credits, @items)",
                    ("@code", voucher.Code), ("@user", userId), ("@credits", voucher.Credits > 0 ? voucher.Credits : null),
                    ("@items", counts.Count > 0 ? string.Join("|", counts) : null));

                await transaction.CommitAsync();
                return true;
            });

            if (!redeemed) return result;
            result.Success = true;

            // Odin keeps an online player's credits and hand in memory.
            if (result.Credits > 0)
                _commandQueue.QueueCommand(CommandQueueType.update_credits, new CommandTemplate { UserId = userId, Credits = result.Credits });
            if (result.Furni.Count > 0)
                _commandQueue.QueueCommand(CommandQueueType.reload_inventory, new CommandTemplate { UserId = userId });
            return result;
        }

        /// <summary>
        /// Puts one furni in the user's hand the way Odin's CatalogueManager.purchase does for a voucher (no extra data):
        /// the special sprite (posters), post-it pads, trophies engraved with the user's name and the date, the room
        /// dimmer's default, teleporters as a linked pair. Returns film to add (camera 2, a film item 5 and no furni).
        /// </summary>
        private async Task<int> GiveFurni(int userId, string username, Part part)
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
                await Execute("INSERT INTO items_teleporter_links (item_id, linked_id) VALUES (@a, @b), (@b, @a)", ("@a", itemId), ("@b", linkedId));
            }

            return definition.Sprite == "camera" ? 2 : 0;
        }

        private async Task<long> InsertItem(int userId, int definitionId, string customData)
        {
            await Execute("INSERT INTO items (user_id, definition_id, custom_data, owned_since) VALUES (@user, @definition, @data, @since)",
                ("@user", userId), ("@definition", definitionId), ("@data", customData), ("@since", DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
            await using var command = Command("SELECT LAST_INSERT_ID()");
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
    }
}
