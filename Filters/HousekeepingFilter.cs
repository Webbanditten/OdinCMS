
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Filters
{
    public class HousekeepingFilter : Attribute, IAsyncActionFilter
    {
        private IUserService _userService;
        private readonly string _fuse;
        // Todo make enum with roles.
        public HousekeepingFilter(string fuse)
        {
            _fuse = fuse;
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
                    var user = await _userService.GetUserById(context.HttpContext.User.Identity.Name);
                    controller.ViewData["user"] = user;

                    // IF fuse doesnt exist in users fuses
                    if (!user.Fuses.Contains("housekeeping"))
                    {
                        context.Result = controller.Challenge();
                        return;
                    }
                    else
                    {
                        if (!user.Fuses.Contains(_fuse.ToLower()))
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
