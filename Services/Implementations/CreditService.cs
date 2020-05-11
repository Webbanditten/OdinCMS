using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class CreditService : ICreditService
    {
        private readonly ICommandQueueService _commandQueueService;
        private readonly DataContext _context;

        public CreditService(ICommandQueueService commandQueueService, DataContext context)
        {
            _commandQueueService = commandQueueService;
            _context = context;
        }

        public async Task<bool> RedeemCode(string code, int userId)
        {
            if (code == null) return false;
            var voucher = await _context.Vouchers.Where(s => s.VoucherCode.ToLower() == code.ToLower()).FirstOrDefaultAsync();
            if(voucher != null)
            {
               if(voucher.ExpiryDate != null && voucher.ExpiryDate < DateTime.Now)
                {
                    // Remove voucher expired

                    return false;
                } else
                {
                    if (!voucher.IsSingleUse)
                    {
                        var voucherHistory = await _context.VoucherHistory.Where(s => s.VoucherCode.ToLower() == code.ToLower() && s.UserId == userId).FirstOrDefaultAsync();
                        if (voucherHistory != null)
                        {
                            return false;
                        }
                    }

                    var user = await _context.Users.Where(s => s.Id == userId).FirstOrDefaultAsync();
                    if (user != null)
                    {
                        // Tell server to update credits if redeemed 
                        _commandQueueService.QueueCommand(Models.Enums.CommandQueueType.update_credits, userId.ToString());

                        // Add to history
                        await _context.VoucherHistory.AddAsync(new VoucherHistory { CreditsRedeemed = voucher.Credits, UsedAt = DateTime.Now, UserId = user.Id, VoucherCode = voucher.VoucherCode });

                        // Give the users the credits 
                        user.Credits += voucher.Credits;
                        _context.Users.Update(user);

                        if (voucher.IsSingleUse)
                        {
                            _context.Vouchers.Remove(voucher);
                        }
                        await _context.SaveChangesAsync();

                        return true;
                    }
                    return false;
                }
            } else
            {
                return false;
            }
        }
    }

}
