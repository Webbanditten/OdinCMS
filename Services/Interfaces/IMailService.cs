using System.Threading.Tasks;
using KeplerCMS.Models;
using KeplerCMS.Models.Enums;

namespace KeplerCMS.Services.Interfaces
{
    public interface IMailService
    {
        public Task<FluentEmail.Core.Models.SendResponse> SendEmail(string[] to, string subject, string body);
        public Task<FluentEmail.Core.Models.SendResponse> SendForgotPassword(string to, string link);
        public Task<FluentEmail.Core.Models.SendResponse> SendListOfHabboNames(string to, string[] habboNames);
        /// <summary>Staff answered a Habbo Help Tool ticket: the link to read the answer and reply (IotController).</summary>
        public Task<FluentEmail.Core.Models.SendResponse> SendHelpTicketReply(string to, string link);
        /// <summary>A Habbo Christmas card for a friend (EcardController): who sent it and the link to see it.</summary>
        public Task<FluentEmail.Core.Models.SendResponse> SendEcard(string to, string sender, string toName, string headline, string link);
    }
}
