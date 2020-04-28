using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace KeplerCMS.Filters
{
    public class LoggedInFilter : Attribute, IActionFilter
    {
        private bool _redirect;
        private IUserService _userService;
        private IMenuService _menuService;
        public LoggedInFilter(bool redirect = true)
        {
            _redirect = redirect;
        }
        public void OnActionExecuted(ActionExecutedContext context)
        {

        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _userService = context.HttpContext.RequestServices.GetService<IUserService>();
            _menuService = context.HttpContext.RequestServices.GetService<IMenuService>();

            if (context.Controller is Controller controller)
            {
                var menu = _menuService.GetMenu();
                controller.ViewData["menu"] = menu;
                if (!context.HttpContext.User.Identity.IsAuthenticated)
                {
                    controller.ViewData["user"] = null;
                    if (_redirect)
                    {
                        context.Result = controller.Challenge();
                    }
                }
                else
                {
                    //injecting values in the ViewData
                    var user = _userService.GetUserById(context.HttpContext.User.Identity.Name);
                    controller.ViewData["user"] = user;
                }
            }
        }
    }
}
