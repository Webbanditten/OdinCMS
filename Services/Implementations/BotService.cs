using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeplerCMS.Areas.Housekeeping.Models.Views;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace KeplerCMS.Services
{
    public class BotService : IBotService
    {
        private readonly DataContext _context;
        private readonly IRoomService _roomService;
        private readonly ICommandQueueService _commandQueueService;

        public BotService(DataContext context, IRoomService roomService, ICommandQueueService commandQueueService)
        {
            _roomService = roomService;
            _context = context;
            _commandQueueService = commandQueueService;
        }

        public async Task<List<Bots>> GetForRoom(int roomId)
        {
            return await _context.Bots.Where(x => x.RoomId == roomId).ToListAsync();
        }

        public async Task<Bots> Get(int botId)
        {
            return await _context.Bots.FirstOrDefaultAsync(x => x.Id == botId);
        }

        public async Task<List<Bots>> GetAllBots()
        {
            var allBots = await _context.Bots.ToListAsync();
            var allBotsWithRooms = new List<Bots>();
            foreach (var bot in allBots)
            {
                var room = await _roomService.GetRoom(bot.RoomId);
                if (room == null) continue;
                bot.Room = room;
                allBotsWithRooms.Add(bot);
            }
            return allBotsWithRooms;
        }

        public async Task<bool> AddBot(Bots bot)
        {
            _context.Bots.Add(bot);
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        [HttpPost]
        public async Task<bool> UpdateBot(Bots bot)
        {
            
            var botData = await Get(bot.Id);
            if (botData == null) return false;
            var oldBotRoomId = botData.RoomId;
            botData.Name = bot.Name;
            botData.Mission = bot.Mission ?? "";
            botData.X = bot.X;
            botData.Y = bot.Y;
            botData.StartLook = bot.StartLook;
            botData.figure = bot.figure;
            botData.Walkspace = bot.Walkspace;
            botData.RoomId = bot.RoomId;
            botData.Speech = bot.Speech ?? "";;
            botData.Response = bot.Response ?? "";;
            botData.UnrecognisedResponse = bot.UnrecognisedResponse ?? "";;
            botData.HandItems = bot.HandItems ?? "";;

            try
            {
                _context.Bots.Update(botData);

                await _context.SaveChangesAsync();

            } catch {
                return false;
            }
            
            // TODO: Fix this shit 

            if (oldBotRoomId != botData.RoomId)
            {
                _commandQueueService.QueueCommand(CommandQueueType.reset_bots, new CommandTemplate { RoomId = oldBotRoomId});
                _commandQueueService.QueueCommand(CommandQueueType.reset_bots, new CommandTemplate { RoomId = bot.RoomId});
            }
            else
            {
                    _commandQueueService.QueueCommand(CommandQueueType.reset_bots, new CommandTemplate { RoomId = bot.RoomId});
            }

            
            
            return true;   
        }

        public async Task<bool> DeleteBot(int botId)
        {
            var bot = await Get(botId);
            if (bot == null) return false;
            
            _context.Bots.Remove(bot);
            await _context.SaveChangesAsync();
            
            return true;
        }
    }
}
