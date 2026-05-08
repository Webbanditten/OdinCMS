
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;

namespace KeplerCMS.Filters
{
    public class HousekeepingFilter : Attribute, IAsyncActionFilter
    {
        private IUserService _userService;
        private List<string> _fuses = new List<string>();
        
        public HousekeepingFilter(Fuse fuse)
        {
            _fuses.Add(fuse.Description().ToLower());
        }
        
        public HousekeepingFilter(Fuse[] fuses)
        {
            foreach (var fuse in fuses)
            {
                _fuses.Add(fuse.Description().ToLower());
            }
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            _userService = context.HttpContext.RequestServices.GetService<IUserService>();
            // _userService get list of fuses for role

            if (context.Controller is Controller controller)
            {

                if (!context.HttpContext.User.Identity.IsAuthenticated)
                {
                    context.Result = controller.Challenge();
                    return;
                } else {
                    //injecting the fuses into view
                    var user = await _userService.GetUserById(int.Parse(context.HttpContext.User.Identity.Name));
                    controller.ViewData["user"] = user;
                    
                    
                    
                    // IF fuse doesnt exist in users fuses
                    if (!user.Fuses.Any(s => _fuses.Contains(s.FuseName)))
                    {
                        context.Result = controller.Challenge();
                        return;
                    }
                    else
                    {
                        if (!user.Fuses.Any(s => _fuses.Contains(s.FuseName)))
                        {
                            context.Result = new RedirectToActionResult("Index", "Home", null);
                            return;
                        }
                    }
                }
            }

            await next();
        }
    }
}
