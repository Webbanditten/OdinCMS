﻿using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Helpers;
using KeplerCMS.Models;
using KeplerCMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services.Implementations
{
    public class FriendService : IFriendService
    {
        private readonly ICommandQueueService _commandQueueService;
        private readonly DataContext _context;
        private readonly IUserService _userService;

        public FriendService(ICommandQueueService commandQueueService, IUserService userService, DataContext context)
        {
            _commandQueueService = commandQueueService;
            _context = context;
            _userService = userService;
        }

        public async Task<List<Users>> GetFriendsWithIdAndUsername(int userId)
        {
            var friends = await GetFriends(userId);
            var friendsWithDetails = new List<Users>();
            foreach(var friend in friends)
            {
                Users friendDetails = null;
                if(friend.ToId != userId)
                {
                    var getFriend = (from u in _context.Users
                                     where u.Id == friend.ToId
                                     select new { u.Id, u.Username }).FirstOrDefault();
                    if(getFriend != null)
                        friendDetails = new Users { Id = getFriend.Id, Username = getFriend.Username };
                    
                } else if(friend.FromId != userId)
                {
                    var getFriend = (from u in _context.Users
                                     where u.Id == friend.FromId
                                     select new { u.Id, u.Username }).FirstOrDefault();
                    if (getFriend != null)
                        friendDetails = new Users { Id = getFriend.Id, Username = getFriend.Username };
                }
                if(!friendsWithDetails.Any(s=>friendDetails != null && s.Id == friendDetails.Id))
                {
                    friendsWithDetails.Add(friendDetails);
                }
            }
            return friendsWithDetails;
        }

        public async Task<FriendRequests> AddFriend(int from, int to)
        {
            if (!(await RequestExists(from, to))) {

                _commandQueueService.QueueCommand(Models.Enums.CommandQueueType.send_friend_request, new CommandTemplate { UserId = from, FriendRequestTo = to });
                var friendRequest = new FriendRequests { FromId = from, ToId = to };
                _context.FriendRequests.Add(friendRequest);
                await _context.SaveChangesAsync();
                return friendRequest;
            }

            return null;
        }

        public async Task<List<Friends>> GetFriends(int userId)
        {
            return await _context.Friends.Where(s => s.FromId == userId || s.ToId == userId).ToListAsync();
        }

        public async Task<List<FriendsViewModel>> GetFriendsWithUserData(int userId)
        {
            var friends = await GetFriends(userId);
            var friendsWithDetails = new List<FriendsViewModel>();
            foreach(var friend in friends)
            {
                Users friendDetails = null;
                if(friend.ToId != userId)
                {
                    var getFriend = await _userService.GetUserById(friend.ToId);
                    if(getFriend != null)
                        friendDetails = getFriend;
                    
                } else if(friend.FromId != userId)
                {
                    var getFriend = await _userService.GetUserById(friend.FromId);
                    if (getFriend != null)
                        friendDetails = getFriend;
                }
                if(friendsWithDetails.Where(s=>s.UserDetails.Id == friendDetails.Id).Count() == 0)
                {
                    friendsWithDetails.Add(new FriendsViewModel { FriendDetails = friend, UserDetails = friendDetails });
                }
            }
            return friendsWithDetails;
        }

        public async Task<bool> IsFriends(int from, int to)
        {
            var request = await _context.Friends.Where(s => (s.FromId == from && s.ToId == to) || (s.FromId == to && s.ToId == from)).FirstOrDefaultAsync();
            if (request != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> RequestExists(int from, int to)
        {
            var request = await _context.FriendRequests.Where(s => s.FromId == from && s.ToId == to).FirstOrDefaultAsync();
            if(request != null)
            {
                return true;
            } else
            {
                return false;
            }
        }
    }

}
