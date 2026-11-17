using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KeplerCMS.Areas.Housekeeping
{
    /// <summary>
    /// Vouchers: codes that give credits, furni or both, redeemed in the game's purse or on the website's credits page.
    /// The page (Views/Vouchers/Index) lists them with their redemptions and talks JSON to the actions below
    /// (wwwroot/housekeeping/js/vouchers.js). Vouchers mint credits, so administrators only.
    /// </summary>
    [Area("Housekeeping")]
    [HousekeepingFilter(Fuse.fuse_administrator_access)]
    public class VouchersController : Controller
    {
        private readonly IVoucherService _vouchers;
        private readonly IAuditLogService _audit;

        public VouchersController(IVoucherService vouchers, IAuditLogService audit)
        {
            _vouchers = vouchers;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["history"] = await _vouchers.GetHistory(null, 100);
            return View(await _vouchers.GetVouchers());
        }

        [HttpGet]
        public async Task<IActionResult> Voucher(string code)
        {
            var voucher = await _vouchers.GetVoucher(code ?? "");
            if (voucher == null) return NotFound();
            return Json(new { voucher, history = await _vouchers.GetHistory(voucher.Code, 200) });
        }

        /// <summary>Catalogue items for the furni picker, hidden ones too (rares, prizes).</summary>
        [HttpGet]
        public async Task<IActionResult> Furni(string q) => Json(await _vouchers.SearchFurni(q));

        public class SaveRequest
        {
            /// <summary>The voucher being edited; empty to create.</summary>
            public string Original { get; set; }
            public VoucherSaveRequest Voucher { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveRequest request)
        {
            if (request?.Voucher == null) return BadRequest("Missing voucher.");
            var editing = !string.IsNullOrWhiteSpace(request.Original);
            var result = await _vouchers.Save(request.Voucher, editing ? request.Original.Trim() : null);
            if (!result.Success) return BadRequest(result.Error);

            var furni = string.Join(", ", request.Voucher.Furni.Where(f => !string.IsNullOrWhiteSpace(f.SaleCode)).Select(f => f.Amount + "x " + f.SaleCode));
            var summary = $"{request.Voucher.Credits} credits" + (furni.Length > 0 ? ", " + furni : "") +
                (request.Voucher.SingleUse ? ", single use" : ", once per user") + (string.IsNullOrWhiteSpace(request.Voucher.Expiry) ? "" : ", expires " + request.Voucher.Expiry);
            var codes = result.Codes.Count > 5 ? string.Join(", ", result.Codes.Take(5)) + $" (+{result.Codes.Count - 5} more)" : string.Join(", ", result.Codes);
            await _audit.AddLog(editing ? AuditLogType.edit_voucher : AuditLogType.create_voucher, int.Parse(User.Identity.Name), codes, summary);

            return Json(new { codes = result.Codes });
        }

        public class CodeRequest
        {
            public string Code { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] CodeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Code)) return BadRequest("Missing code.");
            if (!await _vouchers.Delete(request.Code.Trim())) return BadRequest("That voucher doesn't exist any more.");
            await _audit.AddLog(AuditLogType.delete_voucher, int.Parse(User.Identity.Name), request.Code.Trim(), null);
            return Json(new { deleted = true });
        }
    }
}
