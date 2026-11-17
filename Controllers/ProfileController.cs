using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Isopoh.Cryptography.Argon2;
using KeplerCMS.Data;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Westwind.Globalization;

namespace KeplerCMS.Controllers
{
    /// <summary>
    /// "Min profil": the look (figure editor), mission, email and password tabs (Views/Profile/Index.cshtml). Each change
    /// writes only its own column, so it can't overwrite what the game changed meanwhile (credits and so on).
    /// </summary>
    [MaintenanceFilter]
    public class ProfileController : Controller
    {
        public const int MissionMaxLength = 50;
        private const int PasswordMinLength = 6, PasswordMaxLength = 32; // as on the registration form

        private readonly IUserService _userService;
        private readonly ISettingsService _settingService;
        private readonly ICommandQueueService _commandQueue;
        private readonly DataContext _context;

        public ProfileController(IUserService userService, ISettingsService settingService, ICommandQueueService commandQueue, DataContext context)
        {
            _settingService = settingService;
            _userService = userService;
            _commandQueue = commandQueue;
            _context = context;
        }

        private int UserId => int.Parse(User.Identity.Name);

        private static string T(string key, string set, string fallback)
        {
            var value = DbRes.T(key, set);
            return string.IsNullOrWhiteSpace(value) || value == key ? fallback : value;
        }

        /// <summary>Back to the profile on the tab that was saved, with a green or red message.</summary>
        private IActionResult Done(string tab, string ok = null, string error = null)
        {
            TempData["profile_tab"] = tab;
            if (ok != null) TempData["profile_ok"] = ok;
            if (error != null) TempData["profile_error"] = error;
            return RedirectToAction("Index");
        }

        [LoggedInFilter]
        public async Task<IActionResult> Index()
        {
            return View(await _settingService.GetAll());
        }

        [LoggedInFilter]
        [HttpPost]
        public IActionResult UpdateFigure(string newGender, string figureData)
        {
            // The figure editor posts the figure ("hd-180-1.ch-210-66..." or the old all-digit kind) and M or F.
            if ((newGender == "M" || newGender == "F") && !string.IsNullOrEmpty(figureData) && figureData.Length <= 255
                && Regex.IsMatch(figureData, "^[A-Za-z0-9.-]+$"))
            {
                _userService.UpdateFigure(User.Identity.Name, figureData, newGender);
            }
            return RedirectToAction("index");
        }

        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> UpdateMission(string mission)
        {
            // Like the game (Odin's StringUtil.filterInput): no control characters.
            mission = new string((mission ?? "").Where(c => c >= ' ').ToArray()).Trim();
            if (mission.Length > MissionMaxLength) mission = mission[..MissionMaxLength];

            await _context.Users.Where(u => u.Id == UserId).ExecuteUpdateAsync(s => s.SetProperty(u => u.Motto, mission));
            // An online Habbo gets the new mission straight away (Odin reloads figure, gender and mission).
            _commandQueue.QueueCommand(CommandQueueType.refresh_appearance, new CommandTemplate { UserId = UserId });
            return Done("mission", ok: T("mission_saved", "profile", "Din mission er gemt."));
        }

        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> UpdateEmail(string email, string password)
        {
            email = (email ?? "").Trim();
            var user = await _context.Users.AsNoTracking().Where(u => u.Id == UserId).Select(u => new { u.Password, u.Email }).FirstOrDefaultAsync();
            if (user == null) return RedirectToAction("Index");
            if (string.IsNullOrEmpty(password) || !Argon2.Verify(user.Password, password))
                return Done("email", error: T("wrong_password", "profile", "Adgangskoden passer ikke."));
            if (email.Length > 256 || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return Done("email", error: T("error_email", "register", "Du skal indtaste en gyldig email adresse!"));

            await _context.Users.Where(u => u.Id == UserId).ExecuteUpdateAsync(s => s.SetProperty(u => u.Email, email));
            return Done("email", ok: T("email_saved", "profile", "Din email er ændret."));
        }

        [LoggedInFilter]
        [HttpPost]
        public async Task<IActionResult> UpdatePassword(string currentPassword, string newPassword, string retypePassword)
        {
            var hash = await _context.Users.AsNoTracking().Where(u => u.Id == UserId).Select(u => u.Password).FirstOrDefaultAsync();
            if (hash == null) return RedirectToAction("Index");
            if (string.IsNullOrEmpty(currentPassword) || !Argon2.Verify(hash, currentPassword))
                return Done("password", error: T("wrong_password", "profile", "Adgangskoden passer ikke."));
            if (newPassword == null || newPassword.Length < PasswordMinLength || newPassword.Length > PasswordMaxLength)
                return Done("password", error: T("error_password_length", "register", "Adgangskoder skal være på mindst 6 tegn."));
            if (newPassword != retypePassword)
                return Done("password", error: T("password_mismatch", "profile", "De to nye adgangskoder er ikke ens."));

            var newHash = Argon2.Hash(newPassword);
            await _context.Users.Where(u => u.Id == UserId).ExecuteUpdateAsync(s => s.SetProperty(u => u.Password, newHash));
            return Done("password", ok: T("password_saved", "profile", "Din adgangskode er ændret."));
        }
    }
}
