using KeplerCMS.Models;
using KeplerCMS.Models.Enums;

namespace KeplerCMS.Services.Interfaces
{
    public interface ICommandQueueService
    {
        public void QueueCommand(CommandQueueType command, CommandTemplate template);
    }
}
