using Isopoh.Cryptography.Argon2;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KeplerCMS.Controllers
{
    [MaintenanceFilter]
    [MenuFilter]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ISettingsService _settingsService;

        public AccountController(IUserService userService, ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Index(string returnUrl)
        {
            return View("index", returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string returnUrl)
        {
            if(username != null && password != null)
            {
                var user = await _userService.GetUserByUsername(username);
                if (user != null && Argon2.Verify(user.Password, password))
                {

                    var claims = new List<Claim> { new Claim(ClaimTypes.Name, user.Id.ToString()) };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        #region cookieoptions
                        /*AllowRefresh = true,
                        //
                        // Refreshing the authentication session should be allowed.

                        //ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(20),
                        // The time at which the authentication ticket expires. A 
                        // value set here overrides the ExpireTimeSpan option of 
                        // CookieAuthenticationOptions set with AddCookie.

                        //IsPersistent = true,
                        // Whether the authentication session is persisted across 
                        // multiple requests. When used with cookies, controls
                        // whether the cookie's lifetime is absolute (matching the
                        // lifetime of the authentication ticket) or session-based.

                        //IssuedUtc = <DateTimeOffset>,
                        // The time at which the authentication ticket was issued.

                        //RedirectUri = <string>
                        // The full path or absolute URI to be used as an http 
                        // redirect response value.*/
                        #endregion
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                    if(returnUrl != null && returnUrl != "")
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("index", "home");
                }
                else
                {

                    ViewData["wrong_users_or_password"] = true;
                    return View("index", returnUrl);
                }
            } else
            {
                ViewData["please_enter_username"] = true;
                return View("index", returnUrl);
            }
        }

        public async Task <IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("index", "home");
        }
        [HttpPost]
        [Route("register/start")]
        public async Task<IActionResult> RegisterStart(RegistrationViewModel model)
        {
            return View(model);
        }
        [HttpPost]
        [Route("register/step/2")]
        public IActionResult RegisterInformation(RegistrationViewModel model)
        {
            return View(model);
        }
        [HttpPost]
        [Route("register/step/3")]
        public IActionResult Email(RegistrationViewModel model)
        {
            return View(model);
        }
        [HttpPost]
        [Route("register/step/4")]
        public IActionResult TermsOfService(RegistrationViewModel model)
        {
            return View(model);
        }
        [HttpPost]
        [Route("register/done")]
        public async Task<IActionResult> Done(RegistrationViewModel model)
        {
            var newUser = await _userService.Create(new Data.Models.Users
            {
                Figure = model.FigureData,
                Username = model.Username,
                Password = model.Password,
                Gender = model.Gender,
                Status = "offline",
                Rank = 1
            });
            // Lets sign the user in if its created
            if(newUser != null)
            {
                var user = await _userService.GetUserById(newUser.Id);
                if (user != null && Argon2.Verify(user.Password, model.Password))
                {

                    var claims = new List<Claim> { new Claim(ClaimTypes.Name, user.Id.ToString()) };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties(); ;

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                }
            }
            if(newUser == null )
            {
                return Redirect("/");
            }
            return View(newUser);
        }
    }
}
