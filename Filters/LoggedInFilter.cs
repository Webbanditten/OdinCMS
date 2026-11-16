using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace KeplerCMS.Filters
{
    // Filter attributes are shared by all requests: request services stay in locals, never in fields
    // (a field let one request use another request's DbContext: "A second operation was started on this context").
    public class LoggedInFilter : Attribute, IAsyncActionFilter
    {
        private readonly bool _redirect;
        public LoggedInFilter(bool redirect = true)
        {
            _redirect = redirect;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var _userService = context.HttpContext.RequestServices.GetService<IUserService>();

            if (context.Controller is Controller controller)
            {
                if (!context.HttpContext.User.Identity.IsAuthenticated)
                {
                    controller.ViewData["user"] = null;
                    if (_redirect)
                    {
                        context.Result = controller.Challenge();
                        return;
                    }
                }
                else
                {
                    //injecting values in the ViewData
                    var user = await _userService.GetUserById(int.Parse(context.HttpContext.User.Identity.Name));
                    controller.ViewData["user"] = user;
                }
            }

            await next();
        }
    }
}