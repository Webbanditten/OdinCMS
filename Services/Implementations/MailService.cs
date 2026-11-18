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
            _fromEmail = string.IsNullOrEmpty(configuration.GetSection("keplercms:mailAddress").Value) ? "test@test.com" : configuration.GetSection("keplercms:mailAddress").Value;
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

        public async Task<SendResponse> SendHelpTicketReply(string to, string link)
        {
            string T(string key, string fallback) { var v = DbRes.T(key, "emails"); return string.IsNullOrWhiteSpace(v) || v == key ? fallback : v; }
            var mjml = await _mjmlServices.Render(Template(@$"
                    <mj-text font-size='20px' color='#004979' font-family='Verdana'>{T("help_reply_header", "Du har fået svar fra Habbo")}</mj-text>
                    <mj-text font-size='12px' color='#004979' font-family='Verdana'>{T("help_reply_body", "Vi har svaret på din henvendelse til Habbo Hjælp. Læs svaret og skriv tilbage her: {link}").Replace("{link}", "<a href=\"" + link + "\">" + link + "</a>")}</mj-text>
                "));

            return await SendEmail(new string[] { to }, T("help_reply_subject", "Svar på din henvendelse til Habbo"), mjml.Html);
        }

        public async Task<SendResponse> SendEcard(string to, string sender, string toName, string headline, string link)
        {
            string T(string key, string fallback) { var v = DbRes.T(key, "emails"); return string.IsNullOrWhiteSpace(v) || v == key ? fallback : v; }
            string E(string s) => System.Net.WebUtility.HtmlEncode(s ?? "");
            var greeting = string.IsNullOrWhiteSpace(toName) ? T("ecard_hello_anon", "Hej!") : T("ecard_hello", "Hej {name}!").Replace("{name}", E(toName));
            var mjml = await _mjmlServices.Render(Template(@$"
                    <mj-text font-size='20px' color='#004979' font-family='Verdana'>{T("ecard_header", "Du har fået et Habbo-julekort!")}</mj-text>
                    <mj-text font-size='12px' color='#004979' font-family='Verdana'>{greeting} {T("ecard_body", "{sender} har lavet et julekort til dig på Habbo Danmark.").Replace("{sender}", "<b>" + E(sender) + "</b>")}</mj-text>
                    {(string.IsNullOrWhiteSpace(headline) ? "" : "<mj-text font-size='16px' color='#b2001f' font-family='Verdana'><i>" + E(headline) + "</i></mj-text>")}
                    <mj-button background-color='#e7a33a' color='#ffffff' font-family='Verdana' font-weight='bold' href='{E(link)}'>{T("ecard_button", "Se dit julekort")}</mj-button>
                    <mj-text font-size='10px' color='#666666' font-family='Verdana'>{T("ecard_footer", "Du får denne e-mail, fordi en Habbo skrev din adresse på et julekort. Vi gemmer ikke din adresse.")}</mj-text>
                "));
            return await SendEmail(new string[] { to }, T("ecard_subject", "{sender} har sendt dig et Habbo-julekort!").Replace("{sender}", sender ?? "En Habbo"), mjml.Html);
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
