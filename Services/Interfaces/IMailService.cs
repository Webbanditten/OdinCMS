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
    }
}
