using Microsoft.AspNetCore.Mvc;
using KeplerCMS.Filters;
using KeplerCMS.Models;
using Westwind.Globalization;
using System.Globalization;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using System.Threading.Tasks;

namespace KeplerCMS.Areas.Housekeeping
{
    [Area("Housekeeping")]
    public class LocalizationController : Controller
    {
        private readonly CultureInfo _defaultLocale;
        private readonly DbResourceDataManager _dbResourceDataManager;
        public LocalizationController()
        {
            _dbResourceDataManager = DbResourceDataManager.CreateDbResourceDataManager();
            _defaultLocale = System.Threading.Thread.CurrentThread.CurrentCulture;
        }

        [HousekeepingFilter(Fuse.housekeeping_localization)]
        public IActionResult Index()
        {
            var allResources = this._dbResourceDataManager.GetAllResources();
            return View(allResources);
        }

        [HousekeepingFilter(Fuse.housekeeping_localization)]
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] LocalizationModel model)
        {  
            var locale = string.IsNullOrEmpty(model.LocaleId) ? _defaultLocale.ToString() : model.LocaleId;
            if(string.IsNullOrEmpty(model.LocaleId)) {
                var item = _dbResourceDataManager.GetResourceItem(model.ResourceId, model.ResourceSet, "");
                item.Value = model.Value;
                item.LocaleId = locale;
                _dbResourceDataManager.DeleteResource(item.ResourceId, item.ResourceSet, "");
                _dbResourceDataManager.UpdateOrAddResource(item);
            } else {
                _dbResourceDataManager.UpdateResource(model.ResourceId, model.Value, locale, model.ResourceSet);
            }
            
            DbResourceConfiguration.ClearResourceCache();
            return Content("OK");
        }
    }
}
