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
   
    public class ChatLogHub : Hub
    {
        // Dictionary to keep track of connection IDs and their respective room IDs
        private static readonly Dictionary<string, string> ConnectionRoomMapping = new ();

        private async Task JoinRoom(string roomId)
        {
            // Add the connection ID to the specified group
            await Groups.AddToGroupAsync(Context.ConnectionId, "room_" + roomId);

            // Track the connection ID and room ID mapping
            ConnectionRoomMapping[Context.ConnectionId] = roomId;
        }

        private async Task LeaveRoom(string roomId)
        {
            // Remove the connection ID from the specified group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "room_" + roomId);

            // Remove the mapping
            ConnectionRoomMapping.Remove(Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Get the room ID for the connection ID
            if (ConnectionRoomMapping.TryGetValue(Context.ConnectionId, out var roomId))
            {
                // Remove the connection ID from the group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "room_" + roomId);

                // Remove the mapping
                ConnectionRoomMapping.Remove(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
        
        public override async Task<Task> OnConnectedAsync()
        {
            // We need to check if the user is authenticated and has the necessary permissions to join the room
            var httpContext = Context.GetHttpContext();
            if (httpContext is { User.Identity.Name: not null })
            {
                var userService = httpContext.RequestServices.GetService<IUserService>();
                var roomId = httpContext.Request.Query["roomId"].ToString();
                try {
                    var userId = int.Parse(httpContext.User.Identity.Name);
                    var user = await userService.GetUserById(userId);
                    if (user is not null)
                    {
                        var hasFuse = user.Fuses.Any(s => s.FuseName == Fuse.fuse_private_rooms.Description().ToLower() || s.FuseName == Fuse.fuse_administrator_access.Description().ToLower());

                        if(string.IsNullOrEmpty(roomId) || !hasFuse)
                        {
                            Context.Abort();
                            return Task.CompletedTask;
                        }
                    }
                
                

                } catch (Exception e) {
                    Console.WriteLine(e);
                }
                await JoinRoom(roomId);
            }
            else {
                
                Context.Abort();
                return Task.CompletedTask;
            }

            return base.OnConnectedAsync();
        }
    }
}