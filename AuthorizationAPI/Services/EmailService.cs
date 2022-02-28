using AuthorizationAPI.Helpers;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace AuthorizationAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly AuthSettings _appSettings;

        public EmailService(IOptions<AuthSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public void Send(string to, string subject, string html, string from = null)
        {
            throw new NotImplementedException();
        }
    }
}
