using System.Collections.Generic;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping.Models.Views;

public class HousekeepingMenuItem
{
    public string Url { get; set; }
    public string Title { get; set; }
    public bool IsActive { get; set; }
    public IEnumerable<string> UserFuses { get; set; }
    public IEnumerable<Fuse> Fuses { get; set; }
}