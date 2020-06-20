using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;
using KeplerCMS.Services.Interfaces;
using Newtonsoft.Json;
using System;

namespace KeplerCMS.Services
{
    public class CommandQueueService : ICommandQueueService
    {
        private readonly DataContext _context;

        public CommandQueueService(DataContext context)
        {
            _context = context;
        }
        public void QueueCommand(CommandQueueType command, CommandTemplate template)
        {
            var json = JsonConvert.SerializeObject(template);
            _context.CommandQueue.Add(new CommandQueue { Executed = 0, Command = Enum.GetName(typeof(CommandQueueType), command), Arguments = json });
            _context.SaveChanges();
        }
    }
}
