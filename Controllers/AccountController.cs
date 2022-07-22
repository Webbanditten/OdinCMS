using Isopoh.Cryptography.Argon2;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text;
using System.Net.Mime;
using FluentEmail.Core;
using System.IO;

namespace KeplerCMS.Controllers
{
    [MaintenanceFilter]
    [MenuFilter]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ISettingsService _settingsService;
        private readonly IConfiguration _configuration;
        private readonly IMailService _mailService;

        public AccountController(IConfiguration configuration, IUserService userService, ISettingsService settingsService, IMailService mailService)
        {
            _configuration = configuration;
            _settingsService = settingsService;
            _userService = userService;
            _mailService = mailService;
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
            if (username != null && password != null)
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
                    if (returnUrl != null && returnUrl != "")
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
            }
            else
            {
                ViewData["please_enter_username"] = true;
                return View("index", returnUrl);
            }
        }

        public async Task<IActionResult> Logout()
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
                Rank = 1,
                Email = model.Email
            });
            // Lets sign the user in if its created
            if (newUser != null)
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
            if (newUser == null)
            {
                return Redirect("/");
            }
            return View(newUser);
        }
        [HttpGet]
        public async Task<IActionResult> Forgot()
        {
            return View();
        }
        [HttpGet]
        [Route("account/forgot/reset/{guid}")]
        public async Task<IActionResult> ResetPassword(string guid)
        {
            var valid = false;
            var user = await _userService.ValidatePasswordResetLink(guid);
            if(user != null) {
                valid = true;
            }
            return View(new ResetPasswordViewModel { Valid = valid, Code = guid });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("account/forgot/reset")]
        public async Task<IActionResult> ResetPassword(string code, string password)
        {
            var success = await _userService.ResetPassword(code, password);
            if(success) return View("ResetPasswordSuccess");
            
            return View(new ResetPasswordViewModel { Valid = false });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("account/forgot/habboname")]
        public async Task<IActionResult> ForgotHabboName(string email)
        {
            ViewData["success_sent_habbonames"] = true;
            var users = await _userService.GetUsersByEmail(email);
            if(users.Length > 0) {
                List<string> habboNames = new List<string>();
                foreach (var user in users)
                {
                    habboNames.Add(user.Username);
                }
                await _mailService.SendListOfHabboNames(email, habboNames.ToArray());
            }
            return View("forgot");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("account/forgot/password")]
        public async Task<IActionResult> ForgotHabboName(string username, string email)
        {
            ViewData["success_sent_forgot_password"] = true;
            var user = await _userService.GetUserByUsername(username);
            if(user != null && user.Email.ToLower() == email.ToLower()) {
                var link = await _userService.GeneratePasswordResetLink(user.Id);
                await _mailService.SendForgotPassword(email, link);
            }
            return View("forgot");
        }
    }
}
