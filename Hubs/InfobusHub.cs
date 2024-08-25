using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Filters;
using KeplerCMS.Helpers;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace KeplerCMS.Hubs
{
   
    public class InfobusHub : Hub
    {
        
        public override async Task<Task> OnConnectedAsync()
        {
            // We need to check if the user is authenticated and has the necessary permissions to join the room
            var httpContext = Context.GetHttpContext();
            if (httpContext is { User.Identity.Name: not null })
            {
                var userService = httpContext.RequestServices.GetService<IUserService>();
                try {
                    var userId = int.Parse(httpContext.User.Identity.Name);
                    var user = await userService.GetUserById(userId);
                    if (user is not null)
                    {
                        var hasFuse = user.Fuses.Any(s => s.FuseName == Fuse.fuse_infobus.Description().ToLower());

                        if(!hasFuse)
                        {
                            Context.Abort();
                            return Task.CompletedTask;
                        }
                    }
                
                

                } catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
            else {
                
                Context.Abort();
                return Task.CompletedTask;
            }

            return base.OnConnectedAsync();
        }
    }
}