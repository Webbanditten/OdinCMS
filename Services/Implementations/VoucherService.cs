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
        public const int MaxCodes = 500;
        public const int MaxItems = 100;
        private static readonly Regex CodePattern = new Regex("^[A-Za-z0-9_-]{3,100}$");

        private readonly DataContext _context;
        private readonly ContextSql _sql;
        private readonly ICatalogueFurniService _catalogue;
        private readonly ICommandQueueService _commandQueue;
        private readonly ILogger<VoucherService> _logger;

        public VoucherService(DataContext context, ICatalogueFurniService catalogue, ICommandQueueService commandQueue, ILogger<VoucherService> logger)
        {
            _context = context;
            _sql = new ContextSql(context);
            _catalogue = catalogue;
            _commandQueue = commandQueue;
            _logger = logger;
        }

        private async Task<Dictionary<string, CatalogueFurniEntry>> LoadCatalogue() => (await _catalogue.Load()).BySaleCode;

        // ---- Housekeeping ----

        public async Task<List<VoucherView>> GetVouchers() => await LoadVouchers(null);

        public async Task<VoucherView> GetVoucher(string code) => (await LoadVouchers(code)).FirstOrDefault();

        private async Task<List<VoucherView>> LoadVouchers(string code)
        {
            var catalogue = await LoadCatalogue();
            return await _sql.InConnection(async () =>
            {
                var filter = code == null ? "" : " WHERE voucher_code = @code";
                var vouchers = await _sql.Query("SELECT voucher_code, credits, expiry_date, is_single_use FROM vouchers" + filter + " ORDER BY voucher_code",
                    r => new VoucherView
                    {
                        Code = r.GetString(0),
                        Credits = r.GetInt32(1),
                        ExpiryDate = r.IsDBNull(2) ? null : r.GetDateTime(2),
                        IsSingleUse = Convert.ToInt32(r.GetValue(3)) == 1,
                    }, ("@code", code));
                var items = await _sql.Query("SELECT voucher_code, catalogue_sale_code FROM vouchers_items" + filter,
                    r => (Code: r.GetString(0), SaleCode: r.GetString(1)), ("@code", code));
                var counts = await _sql.Query("SELECT voucher_code, COUNT(*) FROM vouchers_history" + filter + " GROUP BY voucher_code",
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
                        Sprite = entry?.ImageSprite,
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
                .OrderBy(e => e.NotGivable != null).ThenBy(e => e.IsHidden).ThenBy(e => e.Name)
                .Take(60)
                .Select(e => new VoucherFurni
                {
                    SaleCode = e.SaleCode,
                    Name = e.Name,
                    Sprite = e.ImageSprite,
                    Colour = e.Parts.FirstOrDefault()?.Definition?.Colour,
                    Price = e.Price,
                    IsPackage = e.IsPackage,
                    IsHidden = e.IsHidden,
                    NotAllowed = e.NotGivable,
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
                if (entry.NotGivable != null) return fail($"\"{entry.Name}\" can't be given by a voucher: {entry.NotGivable.ToLowerInvariant()}.");
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

            return await _sql.InConnection(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                if (existingCode != null)
                {
                    var updated = await _sql.Execute("UPDATE vouchers SET credits = @credits, expiry_date = @expiry, is_single_use = @single WHERE voucher_code = @code",
                        ("@credits", request.Credits), ("@expiry", expiry), ("@single", request.SingleUse ? 1 : 0), ("@code", existingCode));
                    if (updated == 0)
                    {
                        // MariaDB reports 0 for an UPDATE that changes nothing: tell that apart from a missing voucher.
                        var exists = await _sql.Query("SELECT 1 FROM vouchers WHERE voucher_code = @code", r => 1, ("@code", existingCode));
                        if (exists.Count == 0) return fail("That voucher doesn't exist any more (redeemed or deleted).");
                    }
                    await _sql.Execute("DELETE FROM vouchers_items WHERE voucher_code = @code", ("@code", existingCode));
                    codes.Add(existingCode);
                }
                else
                {
                    if (codes.Count == 1)
                    {
                        var taken = await _sql.Query("SELECT 1 FROM vouchers WHERE voucher_code = @code", r => 1, ("@code", codes[0]));
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
                            var taken = await _sql.Query("SELECT 1 FROM vouchers WHERE voucher_code = @code", r => 1, ("@code", candidate));
                            if (taken.Count == 0) codes.Add(candidate);
                        }
                    }

                    foreach (var code in codes)
                    {
                        await _sql.Execute("INSERT INTO vouchers (voucher_code, credits, expiry_date, is_single_use) VALUES (@code, @credits, @expiry, @single)",
                            ("@code", code), ("@credits", request.Credits), ("@expiry", expiry), ("@single", request.SingleUse ? 1 : 0));
                    }
                }

                foreach (var code in codes)
                {
                    foreach (var line in furni)
                    {
                        for (var i = 0; i < line.Amount; i++)
                        {
                            await _sql.Execute("INSERT INTO vouchers_items (voucher_code, catalogue_sale_code) VALUES (@code, @sale)", ("@code", code), ("@sale", line.SaleCode));
                        }
                    }
                }

                await transaction.CommitAsync();
                return new VoucherSaveResult { Success = true, Codes = codes };
            });
        }

        public async Task<bool> Delete(string code)
        {
            return await _sql.InConnection(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                var deleted = await _sql.Execute("DELETE FROM vouchers WHERE voucher_code = @code", ("@code", code));
                await _sql.Execute("DELETE FROM vouchers_items WHERE voucher_code = @code", ("@code", code));
                await transaction.CommitAsync();
                return deleted > 0;
            });
        }

        public async Task<List<VoucherHistoryView>> GetHistory(string code = null, int limit = 200)
        {
            return await _sql.InConnection(() => _sql.Query(
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

            var redeemed = await _sql.InConnection(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                // The same claim as Odin's PurseDao.redeemVoucher: lock the voucher row, check, delete or log, commit.
                var vouchers = await _sql.Query("SELECT voucher_code, credits, is_single_use FROM vouchers WHERE voucher_code = @code " +
                    "AND (expiry_date IS NULL OR expiry_date > NOW()) FOR UPDATE",
                    r => (Code: r.GetString(0), Credits: r.GetInt32(1), SingleUse: Convert.ToInt32(r.GetValue(2)) == 1), ("@code", code));
                if (vouchers.Count == 0) return false;
                var voucher = vouchers[0];

                if (!voucher.SingleUse)
                {
                    var used = await _sql.Query("SELECT 1 FROM vouchers_history WHERE voucher_code = @code AND user_id = @user LIMIT 1", r => 1, ("@code", voucher.Code), ("@user", userId));
                    if (used.Count > 0) return false;
                }

                var saleCodes = await _sql.Query("SELECT catalogue_sale_code FROM vouchers_items WHERE voucher_code = @code", r => r.GetString(0), ("@code", voucher.Code));
                var username = (await _sql.Query("SELECT username FROM users WHERE id = @user", r => r.GetString(0), ("@user", userId))).FirstOrDefault();
                if (username == null) return false;

                if (voucher.SingleUse)
                {
                    await _sql.Execute("DELETE FROM vouchers WHERE voucher_code = @code", ("@code", voucher.Code));
                    await _sql.Execute("DELETE FROM vouchers_items WHERE voucher_code = @code", ("@code", voucher.Code));
                }

                if (voucher.Credits > 0)
                {
                    await _sql.Execute("UPDATE users SET credits = credits + @credits WHERE id = @user", ("@credits", voucher.Credits), ("@user", userId));
                    result.Credits = voucher.Credits;
                }

                var film = 0;
                foreach (var saleCode in saleCodes)
                {
                    if (!catalogue.TryGetValue(saleCode, out var entry) || entry.NotGivable != null)
                    {
                        _logger.LogError("Voucher {Code}: can't give sale code {SaleCode} ({Reason})", voucher.Code, saleCode, entry?.NotGivable ?? "no such catalogue item");
                        continue;
                    }
                    foreach (var part in entry.Parts)
                    {
                        for (var i = 0; i < part.Amount; i++)
                        {
                            film += await _catalogue.Give(userId, username, part);
                        }
                    }
                    result.Furni.Add(entry.Name);
                }

                if (film > 0)
                {
                    await _sql.Execute("UPDATE users SET film = film + @film WHERE id = @user", ("@film", film), ("@user", userId));
                }

                var counts = saleCodes.GroupBy(s => s, StringComparer.Ordinal).Select(g => g.Count() + "," + g.Key).ToList();
                await _sql.Execute("INSERT INTO vouchers_history (voucher_code, user_id, credits_redeemed, items_redeemed) VALUES (@code, @user, @credits, @items)",
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
    }
}
