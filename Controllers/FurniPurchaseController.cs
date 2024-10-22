﻿using Isopoh.Cryptography.Argon2;
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
        }
    }
}
