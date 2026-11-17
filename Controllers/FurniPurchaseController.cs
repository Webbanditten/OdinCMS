using Isopoh.Cryptography.Argon2;
using KeplerCMS.Filters;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Westwind.Globalization;

namespace KeplerCMS.Controllers
{
    [MaintenanceFilter]
    public class FurniPurchaseController : Controller
    {
        private readonly IFurniShopService _shop;

        public FurniPurchaseController(IFurniShopService shop)
        {
            _shop = shop;
        }

        /// <summary>A localized text, or the Danish fallback while the key isn't in cms_localizations.</summary>
        private static string Text(string key, string fallback)
        {
            var value = DbRes.T(key, "shared");
            return string.IsNullOrEmpty(value) || value == key ? fallback : value;
        }

        private ContentResult Message(string text) => Content("<p style=\"height:30px;\">" + System.Net.WebUtility.HtmlEncode(text) +
            " <a href=\"#\" class=\"colorlink arrow\" onclick=\"closePurchaseDialog(); return false;\" style =\"margin-top:10px\" ><span>" + DbRes.T("close", "dialogs") + "</span></a></p>");

        [LoggedInFilter(false)]
        [HttpPost]
        [Route("furnipurchase/purchase_confirmation")]
        public async Task<IActionResult> PurchaseConfirmation(int product)
        {
            // The {buy_furni_ID} widget and older hand-written "buy-btn" links post here: one click buys. Only what any
            // player can buy in the game's catalogue is for sale (FurniShopService).
            if (!User.Identity.IsAuthenticated) return Message(DbRes.T("need_to_be_signed_in", "shared"));
            return await _shop.Purchase(product, int.Parse(User.Identity.Name)) switch
            {
                FurniPurchaseOutcome.Bought => Message(DbRes.T("furni_purchase_success", "shared")),
                FurniPurchaseOutcome.NotEnoughCredits => Message(DbRes.T("not_enough_credits", "shared")),
                _ => Message(Text("furni_not_for_sale", "Dette møbel kan ikke købes på websitet.")),
            };
        }
    }
}
