using KeplerCMS.Data;
using KeplerCMS.Data.Models;
using KeplerCMS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeplerCMS.Services
{
    public class CommandQueueService
    {
        private readonly DataContext _context;

        public CommandQueueService(DataContext context)
        {
            _context = context;
        }
        public void QueueCommand(CommandQueueType command, string arguments)
        {
            _context.CommandQueue.Add(new CommandQueue { Executed = 0, Command = Enum.GetName(typeof(CommandQueueType), command), Arguments = arguments });
            _context.SaveChanges();
        }
    }
}
