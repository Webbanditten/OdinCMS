using System.Collections.Generic;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using KeplerCMS.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeplerCMS.Areas.Housekeeping.Helpers;

public class HousekeepingMenuItemHelper
{
    private readonly List<string> _userFuses;
    private readonly IHtmlHelper _htmlHelper;

    public HousekeepingMenuItemHelper (List<string> userFuses, IHtmlHelper htmlHelper)
    {
        _htmlHelper = htmlHelper;
        _userFuses = userFuses;
    }

    public async Task<IHtmlContent> Render(string name, string url, List<Fuse> fuses)
    {
        return await Render(name, url, fuses, false);
    }
    public async Task<IHtmlContent> Render (string name, string url, List<Fuse> fuses, bool isActive)
    {
        var menuItem = new HousekeepingMenuItemModel
        {
            Name = name,
            Url = url,
            IsActive = isActive,
            Fuses = fuses,
            UserFuses = _userFuses
        };
        
        return await _htmlHelper.PartialAsync("_HousekeepingMenuItem", menuItem);
    }
}
