using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.MyHabbo
{
    /// <summary>
    /// Base for the Habbo Home endpoints. Editing mode is the "editid" cookie (set by HabboHomeController.Edit and
    /// the group pages); every change goes through <see cref="EditableHome"/>, which checks the logged-in user may
    /// edit that home. Ids that don't parse count as missing instead of throwing.
    /// </summary>
    public abstract class HomeEditController : Controller
    {
        protected readonly IHomeService HomeService;

        protected HomeEditController(IHomeService homeService)
        {
            HomeService = homeService;
        }

        protected int? CurrentUserId => int.TryParse(User?.Identity?.Name, out var id) ? id : null;

        protected int? EditHomeId => int.TryParse(Request.Cookies["editid"], out var id) ? id : null;

        /// <summary>The home being edited and the user editing it, or null when not allowed.</summary>
        protected async Task<(int HomeId, int UserId)?> EditableHome()
        {
            if (CurrentUserId is not int userId || EditHomeId is not int homeId) return null;
            return await HomeService.CanEditHome(homeId, userId) ? (homeId, userId) : null;
        }

        /// <summary>The legacy home JS reads results from the x-json header.</summary>
        protected void SetJson(string json)
        {
            Response.Headers["x-json"] = json;
        }

        protected ContentResult Error() => Content("ERROR");
    }
}
