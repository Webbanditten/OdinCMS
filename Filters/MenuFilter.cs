using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace KeplerCMS.Filters
{
    public class MenuFilter : Attribute, IAsyncActionFilter
    {
        private IMenuService _menuService;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            _menuService = context.HttpContext.RequestServices.GetService<IMenuService>();

            if (context.Controller is Controller controller)
            {
                var menu = await _menuService.GetMenu();
                controller.ViewData["menu"] = menu;
            }

            await next();
        }
    }
}
