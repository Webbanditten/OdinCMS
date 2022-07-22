using FluentEmail.Core;
using FluentEmail.Core.Models;
using KeplerCMS.Data;
using KeplerCMS.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Mjml.AspNetCore;
using System.Threading.Tasks;
using Westwind.Globalization;

namespace KeplerCMS.Services
{
    public class MailService : IMailService
    {
        private readonly DataContext _context;
        private readonly IMjmlServices _mjmlServices;
        private readonly string _from;
        private readonly string _fromEmail;
        private readonly IFluentEmail _fluentEmail;
        private readonly string _publicUrl;

        public MailService(IConfiguration configuration, IMjmlServices mjmlServices, IFluentEmail fluentEmail)
        {
            _mjmlServices = mjmlServices;
            _fluentEmail = fluentEmail;
            _fromEmail = string.IsNullOrEmpty(configuration.GetSection("keplercms:mailAddress").Value) ? "Test" : configuration.GetSection("keplercms:mailAddress").Value;
            _from = string.IsNullOrEmpty(configuration.GetSection("keplercms:mailFrom").Value) ? "Test <test@test.com>" : configuration.GetSection("keplercms:mailFrom").Value;
            _publicUrl = string.IsNullOrEmpty(configuration.GetSection("keplercms:publicUrl").Value) ? "http://localhost:5000" : configuration.GetSection("keplercms:publicUrl").Value;
        }

        public async Task<FluentEmail.Core.Models.SendResponse> SendEmail(string[] to, string subject, string body)
        {
            Address[] addresses = new Address[to.Length];
            foreach (var address in to)
            {
                addresses = new Address[] { new Address(address) };
            }
            return await _fluentEmail.To(addresses).Body(body, true)
            .Subject(subject)
            .SetFrom(_fromEmail, _from)
            .SendAsync();
        }

        private string Template(string content)
        {
            return @$"
                <mjml>
                    <mj-body>
                        <mj-section>
                            <mj-column>
                                <mj-image width='100px' src='{_publicUrl}/images/logos/habbo_logo_nourl.gif'></mj-image>
                                <mj-divider border-color='#004979'></mj-divider>
                                {content}
                            </mj-column>
                        </mj-section>
                    </mj-body>
                </mjml>";
        }

        public async Task<FluentEmail.Core.Models.SendResponse> SendForgotPassword(string to, string link)
        {
            var mjml = await _mjmlServices.Render(Template(@$"
                    <mj-text font-size='20px' color='#004979' font-family='Verdana'>{DbRes.T("forgot_password_header", "emails")}</mj-text>
                    <mj-text font-size='12px' color='#004979' font-family='Verdana'>{DbRes.T("forgot_password_body", "emails").Replace("{link}", link)}</mj-text>
                "));

            return await SendEmail(new string[] { to }, DbRes.T("forgot_password_subject", "emails"), mjml.Html);
        }

        public async Task<SendResponse> SendListOfHabboNames(string to, string[] habboNames)
        {
            var content = @$"
                <mj-text font-size='20px' color='#004979' font-family='Verdana'>{DbRes.T("forgot_habboname_header", "emails")}</mj-text>
                <mj-text font-size='12px' color='#004979' font-family='Verdana'>{DbRes.T("forgot_habboname_body", "emails")}</mj-text>
            ";
            foreach(var habboName in habboNames) {
                content += $"<mj-text font-size='12px' color='#004979' font-family='Verdana'>{habboName}</mj-text>";
            }
            var mjml = await _mjmlServices.Render(Template(content));

            return await SendEmail(new string[] { to }, DbRes.T("forgot_habboname_subject", "emails"), mjml.Html);
        }
    }
}
