using Microsoft.Extensions.Options;
using MailKit;
using System;

using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;

namespace LeaveManager.Services
{
    public class EmailSender: IEmailSender
    {
        private readonly EmailSetting _emailSettings;

        public EmailSender(IOptions<EmailSetting> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            mimeMessage.To.Add(MailboxAddress.Parse(email));
            mimeMessage.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlMessage };
            mimeMessage.Body = builder.ToMessageBody();

            try
            {
                using var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, _emailSettings.UseSsl)
                        .ConfigureAwait(false);

                await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.Password).ConfigureAwait(false);
                await client.SendAsync(mimeMessage).ConfigureAwait(false);
                await client.DisconnectAsync(true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }


        }
    }
}
