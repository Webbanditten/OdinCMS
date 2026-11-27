using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KeplerCMS.Services
{
    /// <summary>
    /// Which catalogue a player's client loads: the classic one (hh_cat_code, a list of pages) or Habbo r31's
    /// (hh_cat_new: a tree with folders, colours and icons). The client gets it from its external variables, so the
    /// client pages hand a v31 player <c>keplercms:catalogueV31:vars</c> (a wrapper of external_vars.php that swaps the
    /// catalogue casts) instead of the usual file; Odin answers each client in its catalogue's format. Who may choose is
    /// the setting <c>cms.catalogue.choice</c>: <c>off</c> (everyone gets the classic one), <c>staff</c> (ranks holding
    /// the staff catalogue fuse) or <c>everyone</c>. The choice itself is <c>users.catalogue_v31</c> (profile, "Katalog").
    /// </summary>
    public class CatalogueChoice
    {
        public const string Setting = "cms.catalogue.choice";
        public const string StaffFuse = "fuse_catalogue_administrator";

        private readonly DataContext _context;
        private readonly IConfiguration _configuration;

        public CatalogueChoice(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>The v31 catalogue's variables file, or null when it isn't set up (then nobody can choose it).</summary>
        public string V31Vars => _configuration["keplercms:catalogueV31:vars"] is { Length: > 0 } v ? v : null;

        /// <summary>off, staff or everyone (anything else is off).</summary>
        public async Task<string> Mode()
        {
            var value = (await _context.Settings.Where(s => s.Setting == Setting).Select(s => s.Value).FirstOrDefaultAsync() ?? "").Trim().ToLowerInvariant();
            return value == "staff" || value == "everyone" ? value : "off";
        }

        public async Task<bool> MayChoose(Users user)
        {
            if (user == null || V31Vars == null) return false;
            var mode = await Mode();
            if (mode == "everyone") return true;
            if (mode != "staff") return false;
            return await _context.RankRights.AnyAsync(r => r.RankId == user.Rank && r.FuseName == StaffFuse);
        }

        /// <summary>The variables file this player's client should load, or null for the usual one.</summary>
        public async Task<string> VarsFor(Users user) => user != null && user.CatalogueV31 && await MayChoose(user) ? V31Vars : null;
    }
}
