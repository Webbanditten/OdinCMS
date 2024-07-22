using System;
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
   
    public class ChatLogHub : Hub
    {
        public override async Task<Task> OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext is { User.Identity.Name: not null })
            {
                Console.WriteLine("Huh");
                var userService = httpContext.RequestServices.GetService<IUserService>();
                var roomId = httpContext.Request.Query["roomId"].ToString();
                try {
                    Console.WriteLine("ah");
                    var userId = int.Parse(httpContext.User.Identity.Name);
                    var user = await userService.GetUserById(userId);
                    if (user is not null)
                    {
                        Console.WriteLine("wat");
                        var hasFuse = user.Fuses.Any(s => s.FuseName == Fuse.fuse_private_rooms.Description().ToLower() || s.FuseName == Fuse.fuse_administrator_access.Description().ToLower());
                        Console.WriteLine("wat2" + roomId + "." + hasFuse);
                        if(string.IsNullOrEmpty(roomId) || !hasFuse)
                        {
                            Console.WriteLine("defuq?");
                            Context.Abort();
                            return Task.CompletedTask;
                        }
                    }
                
                

                } catch (Exception e) {
                    Console.WriteLine(e);
                }
           
                await Groups.AddToGroupAsync(Context.ConnectionId, "room_" + roomId);
            }
            else {
                
                Context.Abort();
                return Task.CompletedTask;
            }

            return base.OnConnectedAsync();
        }
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}