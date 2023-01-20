using System.Collections.Generic;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping.Models.Views;

public class HousekeepingMenuItemModel
{
    public string Name { get; set; }
    public string Url { get; set; }
    public List<string> UserFuses { get; set; }
    public List<Fuse> Fuses { get; set; }
    public bool IsActive { get; set; }
}