using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Models;

namespace KeplerCMS.Services.Interfaces
{
    public interface IVoucherService
    {
        /// <summary>Every voucher with its furni and how often it was redeemed, newest expiry first.</summary>
        Task<List<VoucherView>> GetVouchers();

        Task<VoucherView> GetVoucher(string code);

        /// <summary>Catalogue items a voucher can give (hidden ones too), by name or sale code.</summary>
        Task<List<VoucherFurni>> SearchFurni(string query);

        /// <summary>
        /// Creates <see cref="VoucherSaveRequest.Count"/> vouchers (random codes when no code is given) or, with
        /// <paramref name="existingCode"/>, replaces that voucher's credits, furni, expiry and use.
        /// </summary>
        Task<VoucherSaveResult> Save(VoucherSaveRequest request, string existingCode = null);

        Task<bool> Delete(string code);

        Task<List<VoucherHistoryView>> GetHistory(string code = null, int limit = 200);

        /// <summary>
        /// Redeems a code on the website: claims it in one transaction (the voucher row locked like Odin does, so the
        /// game and the website can't both use it), gives the credits and furni, and tells Odin to update an online
        /// player's purse and hand.
        /// </summary>
        Task<VoucherRedeemResult> Redeem(string code, int userId);
    }
}
