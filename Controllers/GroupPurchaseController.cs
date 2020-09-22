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
    public class GroupPurchaseController : Controller
    {
        private readonly IUserService _userService;
        private readonly ICreditService _creditService;
        private readonly IHomeService _homeService;

        public GroupPurchaseController(IUserService userService, ICreditService creditService, IHomeService homeService)
        {
            _userService = userService;
            _creditService = creditService;
            _homeService = homeService;
        }

        [LoggedInFilter(false)]
        [HttpPost]
        [Route("grouppurchase/group_create_form")]
        public IActionResult GroupCreateForm(string name, string description)
        {
            if(User.Identity.IsAuthenticated) {
                return View("Form");
            } else {
                return Content(DbRes.T("login", "groups"));
            }
        }

        [LoggedInFilter(false)]
        [HttpPost]
        [Route("grouppurchase/purchase_confirmation")]
        public async Task<IActionResult> PurchaseConfirmation(string name, string description)
        {
            var userId = int.Parse(User.Identity.Name);
            if(await _creditService.Purchase(10, userId))
            {
                var group = await _homeService.InitGroup(name, description, userId);
                return View("Success", group);
            } else
            {
                return Content("<p style=\"height:30px;\">" + DbRes.T("not_enough_credits", "shared") + " <a href=\"#\" class=\"colorlink arrow\" onclick=\"closeGroupPurchase(); return false;\" style =\"margin-top:10px\" ><span>" + DbRes.T("close", "dialogs") + "</span></a></p>");
            }
        }
    }
}
