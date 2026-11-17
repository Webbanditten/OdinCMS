using System;
using System.Collections.Generic;

namespace KeplerCMS.Models
{
    /// <summary>A voucher as housekeeping shows it.</summary>
    public class VoucherView
    {
        public string Code { get; set; }
        public int Credits { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsSingleUse { get; set; }
        public bool IsExpired => ExpiryDate != null && ExpiryDate <= DateTime.Now;
        public List<VoucherFurniLine> Furni { get; set; } = new List<VoucherFurniLine>();
        public int Redemptions { get; set; }
    }

    /// <summary>One furni line of a voucher: a catalogue sale code given <see cref="Amount"/> times.</summary>
    public class VoucherFurniLine
    {
        public string SaleCode { get; set; }
        public int Amount { get; set; }
        public string Name { get; set; }
        public string Sprite { get; set; }
        public string Colour { get; set; }
        /// <summary>No catalogue item has this sale code (the game logs and skips it).</summary>
        public bool Missing { get; set; }
    }

    /// <summary>A catalogue item for the voucher furni picker.</summary>
    public class VoucherFurni
    {
        public string SaleCode { get; set; }
        public string Name { get; set; }
        public string Sprite { get; set; }
        public string Colour { get; set; }
        public int Price { get; set; }
        public bool IsPackage { get; set; }
        public bool IsHidden { get; set; }
        /// <summary>Why a voucher can't give it (null = it can).</summary>
        public string NotAllowed { get; set; }
    }

    public class VoucherSaveRequest
    {
        public string Code { get; set; }
        public int Count { get; set; } = 1;
        public int Credits { get; set; }
        /// <summary>"yyyy-MM-dd HH:mm" from the date picker, empty for no expiry.</summary>
        public string Expiry { get; set; }
        public bool SingleUse { get; set; } = true;
        public List<VoucherFurniLine> Furni { get; set; } = new List<VoucherFurniLine>();
    }

    public class VoucherSaveResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public List<string> Codes { get; set; } = new List<string>();
    }

    public class VoucherHistoryView
    {
        public string Code { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public DateTime UsedAt { get; set; }
        public int? Credits { get; set; }
        /// <summary>vouchers_history.items_redeemed: "count,sale code|count,sale code".</summary>
        public string Items { get; set; }
    }

    public class VoucherRedeemResult
    {
        public bool Success { get; set; }
        public int Credits { get; set; }
        /// <summary>Names of the furni given, one entry per item.</summary>
        public List<string> Furni { get; set; } = new List<string>();
    }
}
