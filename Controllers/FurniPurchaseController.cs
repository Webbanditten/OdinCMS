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
        public async Task<IActionResult> PurchaseConfirmation()
        {
            // TODO: Actually check if the user is logged in, look up catalogitem, then check if the user has enough credits, give the user the item 
            //and queue update in command queue - Otherwise let the user know that they dont have enough coins. Or det item has been delisted/hidden.
            return Content("Hah ... This will work eventually.. I have a shit ton of other shit to deal with.");
        }
    }
}
