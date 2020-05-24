using Isopoh.Cryptography.Argon2;
using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Westwind.Globalization;

namespace KeplerCMS.Controllers
{
    public class FurniPurchaseController : Controller
    {
        private readonly IUserService _userService;
        private readonly ICreditService _creditService;

        public FurniPurchaseController(IUserService userService, ICreditService creditService)
        {
            _userService = userService;
            _creditService = creditService;
        }

        [LoggedInFilter(false)]
        [HttpPost]
        [Route("furnipurchase/purchase_confirmation")]
        public async Task<IActionResult> PurchaseConfirmation(int product)
        {
            if(User.Identity.IsAuthenticated)
            {
                var item = await _creditService.PurchaseFurni(product, int.Parse(User.Identity.Name));
                if (item != null) {
                    return Content("<p style=\"height:30px;\">" + DbRes.T("furni_purchase_success", "shared") + " <a href=\"#\" class=\"colorlink arrow\" onclick=\"closePurchaseDialog(); return false;\" style =\"margin-top:10px\" ><span>" + DbRes.T("close", "dialogs") + "</span></a></p>");
                } else
                {
                    return Content("<p style=\"height:30px;\">" + DbRes.T("not_enough_credits", "shared") + " <a href=\"#\" class=\"colorlink arrow\" onclick=\"closePurchaseDialog(); return false;\" style =\"margin-top:10px\" ><span>" + DbRes.T("close", "dialogs") + "</span></a></p>");
                }
            }
            else
            {
                return Content("<p style=\"height:30px;\">" + DbRes.T("need_to_be_signed_in", "shared") + " <a href=\"#\" class=\"colorlink arrow\" onclick=\"closePurchaseDialog(); return false;\" style =\"margin-top:10px\" ><span>" + DbRes.T("close", "dialogs") + "</span></a></p>");
            }
            // TODO: Actually check if the user is logged in, look up catalogitem, then check if the user has enough credits, give the user the item 
            //and queue update in command queue - Otherwise let the user know that they dont have enough coins. Or det item has been delisted/hidden.
            return Content("Hah ... This will work eventually.. I have a shit ton of other shit to deal with.");
        }
    }
}
