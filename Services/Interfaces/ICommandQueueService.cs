using KeplerCMS.Models.Enums;

namespace KeplerCMS.Services.Interfaces
{
    public interface ICommandQueueService
    {
        public void QueueCommand(CommandQueueType command, string arguments);
    }
}
