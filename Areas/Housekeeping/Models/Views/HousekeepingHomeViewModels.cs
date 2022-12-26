using System.Collections.Generic;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;

namespace KeplerCMS.Areas.Housekeeping.Models.Views;

public class HousekeepingHomeViewModels
{
    public IEnumerable<DefaultSetting> MissingSettings { get; set; }
    public IEnumerable<Users> LatestSignins { get; set; }
    public int MonthlySignups { get; set; }
    public int totalUsers { get; set; }
    public int totalSignedinUsers { get; set; }
    public IEnumerable<Users> LatestSignups { get; set; }
}