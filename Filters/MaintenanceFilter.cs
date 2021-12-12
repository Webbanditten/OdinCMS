using KeplerCMS.Data.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeplerCMS.Filters
{
    public class MaintenanceFilter : Attribute, IAsyncActionFilter
    {
        private IUserService _userService;
        private ISettingsService _settingsService;
        public MaintenanceFilter(bool redirect = true)
        {
            
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _userService = context.HttpContext.RequestServices.GetService<IUserService>();
            _settingsService = context.HttpContext.RequestServices.GetService<ISettingsService>();
            var enableMaintenance = await _settingsService.Get("cms.maintenance");

            if(enableMaintenance != null && enableMaintenance.Value == "1") {
                if (context.Controller is Controller controller)
                {
                    var redirect = true;
                    if (context.HttpContext.User.Identity.IsAuthenticated)
                    {
                        var user = await _userService.GetUserById(int.Parse(context.HttpContext.User.Identity.Name));
                        if(user != null) {
                            if(user.Rank >= 6) {
                                redirect = false;
                            }   
                        }
                    }
                    if (redirect)
                    {
                        context.Result = controller.RedirectToAction("index", "maintenance");
                        return;
                    }
                }
            }
            
           
            await next();
        }
    }
}